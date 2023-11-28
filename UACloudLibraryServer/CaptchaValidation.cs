/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Opc.Ua.Cloud.Library.Interfaces;


    public class CaptchaSettings
    {
        public string SiteVerifyUrl { get; set; }
        public string ClientApiUrl { get; set; }
        public string SecretKey { get; set; }
        public string SiteKey { get; set; }
        public float BotThreshold { get; set; }
        public bool Enabled { get; set; } = false;
    }

    /// <summary>
    /// Structure matches up with Google's response JSON
    /// </summary>
    public class ReCaptchaResponse
    {
        public bool success { get; set; }
        public double score { get; set; }
        public string action { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
        [JsonProperty("error-codes")]
        public List<string> error_codes { get; set; }
    }

    public class CaptchaValidation : Interfaces.ICaptchaValidation
    {
        private readonly ILogger<CaptchaValidation> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CaptchaSettings _captchaSettings;

        public CaptchaValidation(
            ILogger<CaptchaValidation> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _captchaSettings = new CaptchaSettings();
            configuration.GetSection("CaptchaSettings").Bind(_captchaSettings);
        }

        public async Task<string> ValidateCaptcha(string responseToken)
        {
            if (!_captchaSettings.Enabled) return null;

            bool configError = false;
            //check for valid values
            if (_captchaSettings == null) _logger.LogCritical($"ValidateCaptcha|Captcha settings are missing or invalid");
            if (string.IsNullOrEmpty(_captchaSettings.SiteVerifyUrl))
            {
                configError = true;
                _logger.LogCritical($"ValidateCaptcha|Captcha:BaseAddress is missing or invalid");
            }
            if (string.IsNullOrEmpty(_captchaSettings.SecretKey))
            {
                configError = true;
                _logger.LogCritical($"ValidateCaptcha|Captcha:Secret Key is missing or invalid");
            }
            if (string.IsNullOrEmpty(_captchaSettings.SiteKey))
            {
                configError = true;
                _logger.LogCritical($"ValidateCaptcha|Captcha:Site Key is missing or invalid");
            }

            //non-user caused issue...
            if (configError)
            {
                return "The automated Captcha system is not configured. Please contact the system administrator.";
            }

            //var responseToken = Request.Form["reCaptchaResponseToken"];
            if (string.IsNullOrEmpty(responseToken))
            {
                _logger.LogCritical($"ValidateCaptcha|Captcha:responseToken is missing or invalid");
                return "The Captcha client response was incorrect or not supplied. Please contact the system administrator.";
            }

            //make the API call
            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                client.BaseAddress = new Uri(_captchaSettings.SiteVerifyUrl);

                //prepare the request
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, ""))
                {
                    //add the body
                    var parameters = new Dictionary<string, string>{
                        {"secret", _captchaSettings.SecretKey},
                        {"response", responseToken}
                        //{"remoteip", "ip" } <= this is optional
                    };

                    requestMessage.Content = new FormUrlEncodedContent(parameters);

                    //call the api
                    HttpResponseMessage response = await client.SendAsync(requestMessage);

                    //basic error with call
                    if (!response.IsSuccessStatusCode)
                    {
                        var msg = $"{(int)response.StatusCode}-{response.ReasonPhrase}";
                        _logger.LogCritical($"ValidateCaptcha|Error occurred in the API call: {msg}");
                        return "An error occurred validating the Captcha response. Please contact your system administrator.";
                    }

                    //check the reCaptcha response
                    var data = response.Content.ReadAsStringAsync().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                    var recaptchaResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ReCaptchaResponse>(data);
                    if (recaptchaResponse == null)
                    {
                        _logger.LogCritical($"ValidateCaptcha|Expected Google reCaptcha response was null");
                        return "An error occurred retrieving the Captcha response. Please contact your system administrator.";
                    }

                    if (!recaptchaResponse.success)
                    {
                        var errors = string.Join(",", recaptchaResponse.error_codes);
                        _logger.LogCritical($"ValidateCaptcha| Google reCaptcha returned error(s): {errors}");
                        return "Error(s) occurred validating the Captcha response. Please contact your system administrator.";
                    }

                    // check reCaptcha response action
                    //if (recaptchaResponse.action.ToUpper() != expected_action.ToUpper())
                    //{
                    //    //Logging.Log(new Logging.LogItem { Msg = $"Google RecCaptcha action doesn't match:\nExpected action: {expected_action} Given action: {recaptcha_response.action}" }, DefaultLogValues);
                    //    return (recaptchaResponse, false);
                    //}

                    // anything less than 0.5 is a bot
                    if (recaptchaResponse.score < _captchaSettings.BotThreshold)
                    {
                        _logger.LogCritical($"ValidateCaptcha|Bot score: {recaptchaResponse.score} < Threshold: {_captchaSettings.BotThreshold}");
                        return "You are not a human. If you believe this is not correct, please contact your system administrator.";
                    }
                    else
                    {
                        _logger.LogInformation($"ValidateCaptcha|Goggle Bot score: {recaptchaResponse.score} (0 bad, {_captchaSettings.BotThreshold} threshold, 1 good)");
                    }
                    //if we get here, all good.
                    return null;
                }
            }
            catch (Exception ex)
            {
                var msg = $"ValidateCaptcha|Unexpected error occurred in the API call: {ex.Message}";
                _logger.LogCritical(ex, msg);
                return "An unexpected error occurred validating the Captcha response. Please contact your system administrator.";
            }
            finally
            {
                // Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
                client.Dispose();
            }
        }
    }
}
