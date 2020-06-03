using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_Interactive_MultiTarget
{
    internal class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string MSGraphURL;

        // The MSAL Public client app
        private static IPublicClientApplication application;

        private static async Task Main(string[] args)
        {
            // Using appsettings.json for our configuration settings
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            configuration = builder.Build();

            appConfiguration = configuration
                .Get<PublicClientApplicationOptions>();

            MSGraphURL = configuration.GetValue<string>("GraphApiUrl");

            // We intend to obtain a token for Graph for the following scopes (permissions)
            string[] scopes = new[] { "user.read" };

            // Sign-in user using MSAL and obtain an access token for MS Graph
            GraphServiceClient graphClient = await SignInAndInitializeGraphServiceClient(appConfiguration, scopes);

            // Call the /me endpoint of MS Graph
            await CallMSGraph(graphClient);
        }

        private static async Task<String> SignInUserAndGetTokenUsingMSAL(PublicClientApplicationOptions configuration, string[] scopes)
        {
            string authority = string.Concat(configuration.Instance, configuration.TenantId);

            // Initialize the MSAL library by building a public client application
            application = PublicClientApplicationBuilder.Create(configuration.ClientId)
                                                    .WithAuthority(authority)
                                                    .WithDefaultRedirectUri()
                                                    .Build();

           
            AuthenticationResult result;
            try
            {
                var accounts = await application.GetAccountsAsync();
                result = await application.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                 .ExecuteAsync();
            }
            catch (MsalUiRequiredException ex)
            {
                result = await application.AcquireTokenInteractive(scopes)
                 .WithClaims(ex.Claims)
                 .ExecuteAsync();
            }

            return result.AccessToken;
        }

        /// <summary>
        /// Sign in user using MSAL and obtain a token for MS Graph
        /// </summary>
        /// <returns></returns>
        private async static Task<GraphServiceClient> SignInAndInitializeGraphServiceClient(PublicClientApplicationOptions configuration, string[] scopes)
        {
            GraphServiceClient graphClient = new GraphServiceClient(MSGraphURL,
                new DelegateAuthenticationProvider(async (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", await SignInUserAndGetTokenUsingMSAL(configuration, scopes));
                }));

            return await Task.FromResult(graphClient);
        }

        /// <summary>
        /// Call MS Graph and print results
        /// </summary>
        /// <param name="graphClient"></param>
        /// <returns></returns>
        private static async Task CallMSGraph(GraphServiceClient graphClient)
        {
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            Console.WriteLine("-------- Data from call to MS Graph --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
        }
    }
}