using System.Net;

namespace NodesetUploader
{
    /// <summary>
    /// Retries requests the cloud library rejects with 429 Too Many Requests. The library limits each
    /// client IP to a fixed number of API requests per minute, which a bulk upload exceeds.
    /// </summary>
    public sealed class RateLimitRetryHandler : DelegatingHandler
    {
        private const int MaxAttempts = 8;

        // The server's rate-limit window is one minute, so waiting this long always reaches a new window.
        private static readonly TimeSpan s_maxDelay = TimeSpan.FromSeconds(61);

        public RateLimitRetryHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The request content is buffered by the uploader, so the same message can be resent.
            if (request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(false);
            }

            for (int attempt = 1; ; attempt++)
            {
                HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MaxAttempts)
                {
                    return response;
                }

                TimeSpan delay = GetDelay(response, attempt);
                response.Dispose();

                Console.WriteLine($"Rate limit reached, retrying {request.Method} {request.RequestUri?.AbsolutePath} in {delay.TotalSeconds:0} s (attempt {attempt + 1} of {MaxAttempts}).");
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private static TimeSpan GetDelay(HttpResponseMessage response, int attempt)
        {
            TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
            if (retryAfter == null && response.Headers.RetryAfter?.Date is DateTimeOffset date)
            {
                retryAfter = date - DateTimeOffset.UtcNow;
            }

            TimeSpan delay = retryAfter ?? TimeSpan.FromSeconds(15 * attempt);
            if (delay < TimeSpan.FromSeconds(1))
            {
                delay = TimeSpan.FromSeconds(1);
            }

            return delay > s_maxDelay ? s_maxDelay : delay;
        }
    }
}
