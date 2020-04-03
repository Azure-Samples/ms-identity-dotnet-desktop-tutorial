using Console_Interactive_CustomWebUI.CustomWebUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Console_Interactive_CustomWebUI
{
    class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string _authority;

        // Since the browser is started via Process.Start, there is no control over it,
        // So it is recommended to configure a timeout 
        private const int TimeoutWaitingForBrowserMs = 30 * 1000; //30 seconds

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
                                                    .WithRedirectUri(CustomBrowserWebUi.FindFreeLocalhostRedirectUri()) // required for CustomBrowserWebUi
                                                    .Build();

            string[] scopes = new[] { "user.read" };
            AuthenticationResult result;

            try
            {
                var accounts = await app.GetAccountsAsync();

                // Try to acquire an access token from the cache. If an interaction is required, 
                // MsalUiRequiredException will be thrown.
                result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync()
                            .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeoutWaitingForBrowserMs);

                    // Acquiring an access token interactively using custom web UI.
                    result = await app.AcquireTokenInteractive(scopes)
                                .WithCustomWebUi(new CustomBrowserWebUi()) //Using our custom web ui
                                .ExecuteAsync(cancellationTokenSource.Token)
                                .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to acquire a token interactively... ");
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();

                    return;
                }
                
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
    }
}
