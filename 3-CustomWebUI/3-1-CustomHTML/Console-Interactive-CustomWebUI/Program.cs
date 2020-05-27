using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_Interactive_CustomWebUI
{
    class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string _authority;

        // Object with the custom HTML 
        private static SystemWebViewOptions _customWebView = GetCustomHTML();

        static async Task Main(string[] args)
        {
            // Using appsettings.json as our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            _authority = string.Concat(appConfiguration.Instance, appConfiguration.TenantId);

            // Building a public client application
            var app = PublicClientApplicationBuilder.Create(appConfiguration.ClientId)
                                                    .WithAuthority(_authority)
                                                    .WithRedirectUri(appConfiguration.RedirectUri)
                                                    .Build();

            string[] scopes = new[] { "user.read" };
            AuthenticationResult result;

            try
            {
                var accounts = await app.GetAccountsAsync();

                // Try to acquire an access token from the cache. If an interaction is required, 
                // MsalUiRequiredException will be thrown.
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();
            }
            catch (MsalUiRequiredException)
            {
                // Acquiring an access token interactively. MSAL will cache it so we can use AcquireTokenSilent
                // on future calls.
                result = await app.AcquireTokenInteractive(scopes)
                            .WithSystemWebViewOptions(_customWebView) // Using the custom html
                            .ExecuteAsync();
            }

            string graphApiUrl = configuration.GetValue<string>("GraphApiUrl");
            // Instantiating GraphServiceClient and using the access token acquired above.
            var graphClient = GetGraphServiceClient(result.AccessToken, graphApiUrl);

            // Calling the /me endpoint
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Hello {result.Account.Username}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-------- GRAPH RESULT --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
        }

        private static GraphServiceClient GetGraphServiceClient(string accessToken, string graphApiUrl)
        {
            GraphServiceClient graphServiceClient = new GraphServiceClient(graphApiUrl,
                                                                 new DelegateAuthenticationProvider(
                                                                     async (requestMessage) =>
                                                                     {
                                                                         await Task.Run(() =>
                                                                         {
                                                                             requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                                                                         });
                                                                     }));

            return graphServiceClient;
        }

        /// <summary>
        /// Returns a custom HTML for the authorization success or failure, and redirect url. 
        /// For more available options, please inspect the SystemWebViewOptions class.
        /// </summary>
        /// <returns></returns>
        private static SystemWebViewOptions GetCustomHTML()
        {
            return new SystemWebViewOptions
            {
                BrowserRedirectSuccess = new Uri("https://contoso.com"),

                HtmlMessageSuccess = @"<html style='font-family: sans-serif;'>
                                      <head><title>Authentication Complete</title></head>
                                      <body style='text-align: center;'>
                                          <header>
                                              <h1>Custom Web UI</h1>
                                          </header>
                                          <main style='border: 1px solid lightgrey; margin: auto; width: 600px; padding-bottom: 15px;'>
                                              <h2 style='color: limegreen;'>Authentication complete</h2>
                                              <div>You can return to the application. Feel free to close this browser tab.</div>
                                          </main>
    
                                      </body>
                                    </html>",

                HtmlMessageError = @"<html style='font-family: sans-serif;'>
                                  <head><title>Authentication Failed</title></head>
                                  <body style='text-align: center;'>
                                      <header>
                                          <h1>Custom Web UI</h1>
                                      </header>
                                      <main style='border: 1px solid lightgrey; margin: auto; width: 600px; padding-bottom: 15px;'>
                                          <h2 style='color: salmon;'>Authentication failed</h2>
                                          <div><b>Error details:</b> error {0} error_description: {1}</div>
                                          <br>
                                          <div>You can return to the application. Feel free to close this browser tab.</div>
                                      </main>
    
                                  </body>
                                </html>"
            };
        }
    }
}
