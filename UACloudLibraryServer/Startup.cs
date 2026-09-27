/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using AdminShell;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Opc.Ua.Cloud.Library.Authentication;
using Opc.Ua.Configuration;

[assembly: CLSCompliant(false)]
namespace Opc.Ua.Cloud.Library
{
    public class Startup
    {
        // Rate-limit policy applied to DPP services to prevent unauthorized mass data scraping
        // (EN 18239 §5.2(11)/(15)).
        public const string DppRateLimitPolicy = "DppRateLimit";

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // When this server runs behind a TLS-terminating reverse proxy (an
            // ingress controller, for example), the proxy forwards the request over
            // plain HTTP and records the original scheme and client IP in the
            // X-Forwarded-Proto and X-Forwarded-For headers. Without honouring them
            // the server believes the request arrived over HTTP, and anything it
            // derives from the scheme is then wrong:
            //
            //   - ASP.NET Identity builds its login redirect from the scheme it sees,
            //     so it would send browsers an absolute http:// URL and silently
            //     downgrade the connection, putting credentials on the wire in clear.
            //   - Blazor Server derives its websocket URI the same way, so the client
            //     would be told to open ws:// from an https:// page and the browser
            //     would block it as mixed content.
            //
            // Which proxies are trusted is decided by the single
            // Configure<ForwardedHeadersOptions> registration further down, which is
            // driven by Dpp:ForwardedHeaders:*. Deliberately do NOT add a second
            // registration here: configure delegates compose rather than replace, so an
            // unconditional KnownProxies.Clear() here would silently override the
            // configured trust list and leave every caller trusted.

            services.AddControllersWithViews();

            services.AddRazorComponents().AddInteractiveServerComponents();

            // Setup database context for ASP.NetCore Identity Scaffolding
            services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);

            services.AddDefaultIdentity<IdentityUser>(options =>
                options.SignIn.RequireConfirmedAccount = !string.IsNullOrEmpty(Configuration["EmailSenderAPIKey"])
            )
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddTokenProvider<ApiKeyTokenProvider>(ApiKeyTokenProvider.ApiKeyProviderName);

            // Role claims are baked into the authentication cookie at sign-in, so revoking a role in
            // the database does not by itself stop an already-issued cookie from passing the
            // controlled-element and administration checks. AccessController bumps the user's
            // security stamp on revocation; this interval bounds how long a stale cookie survives
            // before the stamp is re-validated and the principal rejected. EN 18239 section 6.3
            // requires emergency revocation to actually take effect, so the 30-minute framework
            // default is too slow - one minute keeps the revocation window short while still
            // avoiding a database round-trip on every single request.
            services.Configure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.FromMinutes(1));

            // Label the account identifier field (and its validation messages) "Username" rather than
            // "Email" while e-mail verification is disabled, matching the conditional syntax check in
            // EmailAddressWhenVerificationEnabledAttribute. Appended last so it wins over [Display].
            services.AddOptions<MvcOptions>().Configure<IOptions<IdentityOptions>>((mvcOptions, identityOptions) =>
                mvcOptions.ModelMetadataDetailsProviders.Add(new UserIdentifierDisplayMetadataProvider(identityOptions)));

            services.AddScoped<UserService>();

            services.AddHttpContextAccessor();
            services.AddSingleton<IAuthorizationHandler, ReadWriteApiKeyHandler>();

            services.AddTransient<CloudLibDataProvider>();

            services.AddTransient<DbFileStorage>();

            services.AddTransient<UAClient>();

            services.AddSingleton<ApplicationInstance>();

            services.AddScoped<AssetAdministrationShellEnvironmentService>();

            services.AddScoped<DPPService>();
            services.AddSingleton<IDppAccessPolicy, DppAccessPolicy>();
            services.AddScoped<IDppAuditLog, DppAuditLog>();
            services.AddSingleton<IDppAuditKeyProvider, DppAuditKeyProvider>();
            services.AddScoped<Controllers.DppAuditFailureFilter>();
            services.AddScoped<IEsdcSigningKeyProvider, EsdcSigningKeyProvider>();

            // Tamper evidence is only useful if something checks it. Registering verification as a
            // health check means an altered or truncated audit log surfaces through normal monitoring
            // instead of waiting for someone to ask. Tagged so liveness probes can skip it: it reads
            // the entire audit table and belongs on a readiness/monitoring schedule.
            services.AddHealthChecks()
                .AddCheck<DppAuditChainHealthCheck>(
                    DppAuditChainHealthCheck.Name,
                    tags: new[] { DppAuditChainHealthCheck.Tag });

            // The signing key must be identical for every request and every instance, so the ESDC
            // service stays a singleton. Its key is resolved once, lazily, through a temporary scope:
            // resolution needs the scoped DbContext, and it must happen after migrations have created
            // the table, which rules out resolving it here during ConfigureServices.
            services.AddSingleton<IEsdcService>(sp => {
                using IServiceScope scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
                var keyProvider = scope.ServiceProvider.GetRequiredService<IEsdcSigningKeyProvider>();
                string privateKeyPem = keyProvider.GetOrCreatePrivateKeyPemAsync().GetAwaiter().GetResult();

                return new RsaEsdcService(sp.GetRequiredService<IConfiguration>(), privateKeyPem);
            });
            services.AddScoped<IDppVersionArchive, DbFileVersionArchive>();

            services.AddScoped<CaptchaValidation>();

            if (!string.IsNullOrEmpty(Configuration["UseSendGridEmailSender"]))
            {
                services.AddTransient<IEmailSender, SendGridEmailSender>();
            }
            else
            {
                services.AddTransient<IEmailSender, PostmarkEmailSender>();
            }

            services.AddLogging(builder => builder.AddConsole());

            // for captcha validation call
            services.AddHttpClient();

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, SignedInUserAuthenticationHandler>("SignedInUserAuthentication", null)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKeyAuthentication", null);

            if (Configuration["Authentication:Microsoft:ClientId"] != null)
            {
                services.AddAuthentication()
                    .AddCookie()
                    .AddMicrosoftAccount(options => {
                        options.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                        options.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                    })
                    .AddMicrosoftIdentityWebApi(Configuration.GetSection("AzureAd"));
            }

            if (Configuration["OAuth2ClientId"] != null)
            {
                services.AddAuthentication()
                    .AddOAuth("OAuth", "OPC Foundation", options => {
                        options.AuthorizationEndpoint = "https://opcfoundation.org/oauth/authorize/";
                        options.TokenEndpoint = "https://opcfoundation.org/oauth/token/";
                        options.UserInformationEndpoint = "https://opcfoundation.org/oauth/me";

                        options.AccessDeniedPath = new PathString("/Account/AccessDenied");
                        options.CallbackPath = new PathString("/Account/ExternalLogin");

                        options.ClientId = Configuration["OAuth2ClientId"];
                        options.ClientSecret = Configuration["OAuth2ClientSecret"];

                        options.SaveTokens = true;

                        options.CorrelationCookie.SameSite = SameSiteMode.Strict;
                        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;

                        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "ID");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "display_name");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "user_email");

                        options.Events = new OAuthEvents {
                            OnCreatingTicket = async context => {
                                List<AuthenticationToken> tokens = (List<AuthenticationToken>)context.Properties.GetTokens();

                                tokens.Add(new AuthenticationToken() {
                                    Name = "TicketCreated",
                                    Value = DateTime.UtcNow.ToString(DateTimeFormatInfo.InvariantInfo)
                                });

                                context.Properties.StoreTokens(tokens);

                                HttpResponseMessage response = await context.Backchannel.GetAsync($"{context.Options.UserInformationEndpoint}?access_token={context.AccessToken}").ConfigureAwait(false);
                                response.EnsureSuccessStatusCode();

                                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                JsonElement user = JsonDocument.Parse(json).RootElement;

                                context.RunClaimActions(user);
                            }
                        };
                    });
            }

            services.AddAuthorization(options => {
                options.AddPolicy("AdministrationPolicy", policy => policy.RequireRole(Roles.Administrator));
            });

            if (Configuration["APIKeyAuth"] != null)
            {
                services.AddAuthorization(options => {
                    options.AddPolicy("ApiPolicy", policy => {
                        policy.AddAuthenticationSchemes("BasicAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("SignedInUserAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("ApiKeyAuthentication").RequireAuthenticatedUser();
                        policy.AddRequirements(new ReadWriteApiKeyRequirement());
                    });
                });
            }
            else
            {
                services.AddAuthorization(options => {
                    options.AddPolicy("ApiPolicy", policy => {
                        policy.AddAuthenticationSchemes("BasicAuthentication").RequireAuthenticatedUser();
                        policy.AddAuthenticationSchemes("SignedInUserAuthentication").RequireAuthenticatedUser();
                    });
                });
            }

            services.AddSwaggerGen(options => {
                options.SwaggerDoc("v1", new OpenApiInfo {
                    Title = "UA Cloud Library REST Service",
                    Version = "v1",
                    Description = "A REST-full interface to the CESMII & OPC Foundation Cloud Library",
                    Contact = new OpenApiContact {
                        Name = "OPC Foundation",
                        Email = "office@opcfoundation.org",
                        Url = new Uri("https://opcfoundation.org/")
                    }
                });

                options.AddSecurityDefinition("basicAuth", new OpenApiSecurityScheme {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic"
                });


                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                    [new OpenApiSecuritySchemeReference("basicAuth", document)] = []
                });


                if (Configuration["APIKeyAuth"] != null)
                {
                    options.AddSecurityDefinition("ApiKeyAuth", new OpenApiSecurityScheme {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "X-API-Key"
                    });

                    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement {
                        [new OpenApiSecuritySchemeReference("ApiKeyAuth", document)] = []
                    });
                }

                options.CustomSchemaIds(type => type.ToString());

                options.EnableAnnotations();
            });

            // Data Protection keys encrypt the authentication cookie, antiforgery
            // tokens and the e-mail confirmation / password reset tokens. They must
            // OUTLIVE the process: if they are lost, every existing cookie and token
            // becomes undecryptable, which signs all users out and makes outstanding
            // reset links fail. Worse, the antiforgery token on an already-open login
            // page can no longer be validated, so the POST is rejected by model
            // validation before the credentials are ever checked - which looks exactly
            // like a wrong password.
            //
            // Directory.GetCurrentDirectory() is the container's working directory and
            // is destroyed on every restart, so the key ring is configurable: point
            // DATA_PROTECTION_KEY_PATH at a mounted volume in any containerised
            // deployment.
            string keyPath = Configuration["DATA_PROTECTION_KEY_PATH"];
            if (string.IsNullOrWhiteSpace(keyPath))
            {
                keyPath = Directory.GetCurrentDirectory();
            }
            else
            {
                Directory.CreateDirectory(keyPath);
            }

            IDataProtectionBuilder dataProtection = services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

            // Deliberately NOT calling SetApplicationName() by default. The application discriminator
            // is part of the key derivation, so changing it invalidates every payload protected under
            // the previous value: on the first upgrade an otherwise-durable key ring would stop being
            // able to decrypt existing cookies, antiforgery tokens and reset/confirmation links. The
            // framework default derives the discriminator from the content root path, which is stable
            // across restarts and identical across replicas of the same image, so the default already
            // gives the cross-replica behaviour this was originally meant to provide.
            //
            // Set DATA_PROTECTION_APPLICATION_NAME only when replicas genuinely need to share a key
            // ring but do not share a content root path. It is a one-time breaking change for the
            // deployment that adopts it: existing users are signed out and outstanding reset and
            // confirmation links stop working, so roll it out in a maintenance window.
            string applicationName = Configuration["DATA_PROTECTION_APPLICATION_NAME"];
            if (!string.IsNullOrWhiteSpace(applicationName))
            {
                dataProtection.SetApplicationName(applicationName);
            }

            services.Configure<IISServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.Configure<KestrelServerOptions>(options => {
                options.AllowSynchronousIO = true;
            });

            services.AddServerSideBlazor();

            // When this server runs behind a TLS-terminating reverse proxy (an
            // ingress controller, for example), the proxy forwards the request over
            // plain HTTP and records the original scheme and client IP in the
            // X-Forwarded-Proto and X-Forwarded-For headers. Without honouring them:
            //
            //   - ASP.NET Identity builds its login redirect from the scheme it sees,
            //     so it would send browsers an absolute http:// URL and silently
            //     downgrade the connection, putting credentials on the wire in clear.
            //   - The rate limiter below partitions on the connection's remote IP,
            //     which would be the proxy's address for every caller - collapsing a
            //     per-client limit into one shared bucket.
            //
            // KnownIPNetworks/KnownProxies are NOT cleared by default. Clearing them makes the
            // middleware accept X-Forwarded-For/-Proto from any caller, so anyone able to reach this
            // server directly could spoof their client IP (evading the per-IP rate limit below) and
            // spoof the scheme used to build redirects. That is only acceptable where the server is
            // genuinely unreachable except through the proxy, which is a deployment property this
            // code cannot verify - so it must be stated explicitly by the operator.
            //
            // Configure whichever matches the deployment:
            //   Dpp:ForwardedHeaders:KnownProxies:0    - specific proxy IP addresses
            //   Dpp:ForwardedHeaders:KnownNetworks:0   - CIDR ranges, e.g. "10.0.0.0/8"
            //   Dpp:ForwardedHeaders:TrustAllProxies   - true only when network-isolated behind a proxy
            services.Configure<ForwardedHeadersOptions>(options => {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

                string[] knownProxies = Configuration.GetSection("Dpp:ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
                string[] knownNetworks = Configuration.GetSection("Dpp:ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];
                bool trustAllProxies = Configuration.GetValue<bool>("Dpp:ForwardedHeaders:TrustAllProxies");

                if (trustAllProxies)
                {
                    // Explicit operator opt-in: the server is stated to be reachable only via the proxy.
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();
                }
                else if (knownProxies.Length > 0 || knownNetworks.Length > 0)
                {
                    // Replace the loopback defaults with exactly the configured trust anchors.
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();

                    foreach (string proxy in knownProxies)
                    {
                        if (System.Net.IPAddress.TryParse(proxy, out System.Net.IPAddress address))
                        {
                            options.KnownProxies.Add(address);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Dpp:ForwardedHeaders:KnownProxies contains '{proxy}', which is not a valid IP address.");
                        }
                    }

                    foreach (string network in knownNetworks)
                    {
                        if (System.Net.IPNetwork.TryParse(network, out System.Net.IPNetwork parsed))
                        {
                            options.KnownIPNetworks.Add(parsed);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Dpp:ForwardedHeaders:KnownNetworks contains '{network}', which is not a valid CIDR network.");
                        }
                    }
                }

                // Otherwise the framework defaults apply (loopback only), which is the safe choice
                // for a server that may be directly reachable.
            });

            // Limit access to DPP services to prevent attacks or unauthorized
            // mass data scraping. Partition the window per client IP so one caller cannot exhaust others.
            int permitPerMinute = Configuration.GetValue<int?>("Dpp:RateLimit:PermitPerMinute") ?? 100;

            // Validated here rather than left to the limiter. The partition factory is lazy, so an
            // invalid value would not surface until the first DPP request and would then throw on a
            // user request instead of failing the deployment - a configuration typo presenting as a
            // runtime fault. FixedWindowRateLimiter requires a positive permit limit.
            if (permitPerMinute <= 0)
            {
                throw new InvalidOperationException(
                    $"Dpp:RateLimit:PermitPerMinute is {permitPerMinute}, but it must be greater than zero. " +
                    "Remove the setting to use the default of 100 requests per minute per client IP.");
            }

            services.AddRateLimiter(options => {
                options.RejectionStatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests;
                options.AddPolicy(DppRateLimitPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        factory: _ => new FixedWindowRateLimiterOptions {
                            PermitLimit = permitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            services.AddHostedService<CloudLibStartupTask>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppDbContext appDbContext, ApplicationInstance uaApp)
        {
            // Must run before anything that reads the request scheme or client IP -
            // UseHttpsRedirection, the authentication middleware and the rate limiter
            // all do. See the ForwardedHeadersOptions note in ConfigureServices.
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UA Cloud Library REST Service");
                c.EnablePersistAuthorization();
            });

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            // Enforce DPP service rate limits (must follow UseRouting so endpoint policies are resolved).
            app.UseRateLimiter();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapBlazorHub();

                endpoints.MapRazorPages();

                // Audit-chain verification, exposed separately from any liveness probe because it
                // reads the whole audit table. Requires administrator rights: the result reveals
                // whether the log has been tampered with, which is not public information, and an
                // unauthenticated caller could otherwise use it to drive repeated full-table scans.
                //
                // Both policies are required, mirroring how the administrative controllers combine a
                // class-level ApiPolicy with a method-level AdministrationPolicy. AdministrationPolicy
                // declares only a role requirement and no authentication schemes, so on its own the
                // endpoint would fall back to the default Identity cookie alone - a monitoring client
                // presenting Basic or API-key credentials would be seen as unauthenticated even though
                // both are supported everywhere else.
                endpoints.MapHealthChecks("/health/dpp-audit", new HealthCheckOptions {
                    Predicate = registration => registration.Tags.Contains(DppAuditChainHealthCheck.Tag)
                }).RequireAuthorization("ApiPolicy", "AdministrationPolicy");
            });
        }

        public class CloudLibStartupTask : IHostedService
        {
            private readonly IServiceProvider _serviceProvider;

            public CloudLibStartupTask(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var uaApp = scope.ServiceProvider.GetRequiredService<ApplicationInstance>();

                uint retryCount = 0;
                while (retryCount < 12)
                {
                    try
                    {
                        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                        await EnsureIsPublishedColumnAsync(dbContext, cancellationToken).ConfigureAwait(false);
                        break;
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Database not yet available or unknown, retrying...");
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                        retryCount++;
                    }
                }

                if (retryCount == 12)
                {
                    // database permanently unavailable
                    throw new InvalidOperationException("Database not available, exiting!");
                }

                await EnsureEsdcSigningKeyAsync(scope.ServiceProvider).ConfigureAwait(false);

                await InitOPCUAClientServerAsync(uaApp).ConfigureAwait(false);
            }

            // Resolving the ESDC service here forces the signing key to be resolved (and, if permitted,
            // generated and persisted) during startup. Failing at boot is deliberate: the alternative is
            // a server that starts cleanly and only reveals it cannot sign when the first DPP is read.
            private static async Task EnsureEsdcSigningKeyAsync(IServiceProvider services)
            {
                var configuration = services.GetRequiredService<IConfiguration>();
                var environment = services.GetRequiredService<IWebHostEnvironment>();
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

                try
                {
                    // Touch the singleton so key resolution happens now.
                    services.GetRequiredService<IEsdcService>();
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, "Failed to resolve the ESDC signing key during startup.");
                    throw;
                }

                if (!string.IsNullOrWhiteSpace(configuration[EsdcSigningKeyProvider.PrivateKeyConfigurationPath]))
                {
                    return;
                }

                // Reached only where the generated key is permitted: Development, or an explicit opt-in.
                logger.LogWarning(
                    "{ConfigPath} is not configured, so ESDCs are signed with a key generated by the server and stored in the database. " +
                    "That key is included in database backups and readable by anything with database access. " +
                    "For production, provide the key from a managed secret store - see the signing key management section of dpp.md. " +
                    "Environment: {EnvironmentName}.",
                    EsdcSigningKeyProvider.PrivateKeyConfigurationPath,
                    environment.EnvironmentName);

                await Task.CompletedTask.ConfigureAwait(false);
            }

            private static async Task EnsureIsPublishedColumnAsync(AppDbContext dbContext, CancellationToken cancellationToken)
            {
                // Gate on the presence of the new partial index so that existing databases
                // (which may already have the column but not the new indexes) get upgraded.
                const string checkSql =
                    "SELECT COUNT(1) FROM pg_indexes " +
                    "WHERE schemaname = current_schema() " +
                    "  AND tablename = 'NamespaceMeta' " +
                    "  AND indexname = 'IX_NamespaceMeta_Published_Nodeset';";

                await using var connection = dbContext.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                await using (var checkCmd = connection.CreateCommand())
                {
                    checkCmd.CommandText = checkSql;
                    var result = await checkCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (result != null && Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0)
                    {
                        return;
                    }
                }

                var assembly = typeof(CloudLibStartupTask).Assembly;
                var resourceName = "Opc.Ua.Cloud.Library.Migrations.Scripts.AddIsPublishedToNamespaceMeta.sql";
                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded SQL script '{resourceName}' not found.");
                using var reader = new StreamReader(stream);
                var script = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

                string serviceUsername = System.Environment.GetEnvironmentVariable("ServiceUsername");
                if (string.IsNullOrWhiteSpace(serviceUsername))
                {
                    serviceUsername = "admin";
                }

                // Escape single quotes for safe inlining into the SQL literal.
                script = script.Replace("{{ServiceUsername}}", serviceUsername.Replace("'", "''", StringComparison.Ordinal), StringComparison.Ordinal);

                Console.WriteLine($"Applying IsPublished column/index migration to NamespaceMeta (service user: '{serviceUsername}')...");
                await using var scriptCmd = connection.CreateCommand();
                scriptCmd.CommandText = script;
                await scriptCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            private async Task InitOPCUAClientServerAsync(ApplicationInstance uaApp)
            {
                try
                {
                    // wait 5 seconds for the HTTP server to complete starting up
                    // for Azure Container Apps, the HTTP server must be started before the OPC UA server
                    await Task.Delay(5000).ConfigureAwait(false);

                    // remove any existing certificate store
                    if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "pki")))
                    {
                        Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "pki"), true);
                    }

                    // load the application configuration
                    ApplicationConfiguration config = await uaApp.LoadApplicationConfigurationAsync(Path.Combine(Directory.GetCurrentDirectory(), "Application.Config.xml"), false).ConfigureAwait(false);

                    // check the application certificate
                    await uaApp.CheckApplicationInstanceCertificatesAsync(false, 0).ConfigureAwait(false);

                    // create cert validator
                    config.CertificateValidator = new CertificateValidator(DefaultTelemetry.Create(builder => builder.AddConsole()));
                    config.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
                    await config.CertificateValidator.UpdateAsync(config).ConfigureAwait(false);

                    Console.WriteLine("OPC UA client/server app started.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("InitOPCUAClientServerAsync: " + ex.Message);
                    return;
                }
            }

            private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
            {
                if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
                {
                    // accept all OPC UA client certificates
                    e.Accept = true;
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                // nothing to do
                return Task.CompletedTask;
            }
        }
    }
}
