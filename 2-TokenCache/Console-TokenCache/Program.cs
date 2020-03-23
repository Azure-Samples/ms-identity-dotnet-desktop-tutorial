using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_TokenCache
{
    class Program
    {
        private static PublicClientApplicationOptions appConfiguration = null;
        private static IConfiguration configuration;
        private static string _authority;

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

            // Building StorageCreationProperties
            var storageProperties =
                 new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.CacheDir, appConfiguration.ClientId)
                 .WithLinuxKeyring(
                     CacheSettings.LinuxKeyRingSchema,
                     CacheSettings.LinuxKeyRingCollection,
                     CacheSettings.LinuxKeyRingLabel,
                     CacheSettings.LinuxKeyRingAttr1,
                     CacheSettings.LinuxKeyRingAttr2)
                 .WithMacKeyChain(
                     CacheSettings.KeyChainServiceName,
                     CacheSettings.KeyChainAccountName)
                 .Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);

            // Subscribing to the CacheChanged event
            cacheHelper.CacheChanged += (object sender, CacheChangedEventArgs eventArgs) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"Cache Changed, Added: {eventArgs.AccountsAdded.Count()} Removed: {eventArgs.AccountsRemoved.Count()}");
                Console.ResetColor();
            };

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
                            .ExecuteAsync();
            }

            string graphApiUrl = configuration.GetValue<string>("GraphApiUrl");
            // Instantiating GraphServiceClient and using the access token acquired above.
            var graphClient = GetGraphServiceClient(result.AccessToken, graphApiUrl);

            // Calling the /me endpoint
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            DisplayGraphResult(result, me);

            while (true)
            {
                // Display menu
                Console.WriteLine("------------ MENU ------------");
                Console.WriteLine("1. Acquire Token Interactive");
                Console.WriteLine("2. Acquire Token Silent");
                Console.WriteLine("3. Display Accounts (reads the cache)");
                Console.WriteLine("4. Clear cache");
                Console.WriteLine("x. Exit app");
                Console.Write("Enter your Selection:");
                char.TryParse(Console.ReadLine(), out var selection);

                try
                {
                    switch (selection)
                    {
                        case '1': // Interactive
                            Console.Clear();
                            result = await app.AcquireTokenInteractive(scopes)
                                        .ExecuteAsync()
                                        .ConfigureAwait(false);

                            graphClient = GetGraphServiceClient(result.AccessToken, graphApiUrl);
                            me = await graphClient.Me.Request().GetAsync();

                            DisplayGraphResult(result, me);
                            break;

                        case '2': // Silent
                            Console.Clear();
                            Console.WriteLine("Acquiring token from the cache");
                            var accounts2 = await app.GetAccountsAsync().ConfigureAwait(false);
                            var firstAccount = accounts2.FirstOrDefault();

                            // this is expected to fail when account is null
                            result = await app.AcquireTokenSilent(scopes, firstAccount)
                                .ExecuteAsync()
                                .ConfigureAwait(false);

                            graphClient = GetGraphServiceClient(result.AccessToken, graphApiUrl);
                            me = await graphClient.Me.Request().GetAsync();

                            DisplayGraphResult(result, me);
                            break;

                        case '3': // Display Accounts
                            Console.Clear();
                            var accounts3 = await app.GetAccountsAsync().ConfigureAwait(false);
                            if (!accounts3.Any())
                            {
                                Console.WriteLine("No accounts were found in the cache.");
                                Console.Write(Environment.NewLine);
                            }

                            foreach (var acc in accounts3)
                            {
                                Console.WriteLine($"Account for {acc.Username}");
                                Console.Write(Environment.NewLine);
                            }
                            break;

                        case '4': // Clear cache
                            Console.Clear();
                            var accounts4 = await app.GetAccountsAsync().ConfigureAwait(false);
                            foreach (var acc in accounts4)
                            {
                                Console.WriteLine($"Removing account for {acc.Username}");
                                Console.Write(Environment.NewLine);
                                await app.RemoveAsync(acc).ConfigureAwait(false);
                            }
                            break;

                        case 'x':
                            return;
                    }

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Exception : " + ex);
                    Console.ResetColor();
                    Console.WriteLine("Hit Enter to continue");

                    Console.Read();
                }
            }
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

        private static void DisplayGraphResult(AuthenticationResult result, User me)
        {
            Console.ForegroundColor = ConsoleColor.Green;

            Console.Write(Environment.NewLine);
            Console.WriteLine($"Hello {result.Account.Username}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-------- GRAPH RESULT --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
            Console.Write(Environment.NewLine);
            Console.WriteLine("------------------------------");
            Console.Write(Environment.NewLine);
            Console.Write(Environment.NewLine);
            Console.ResetColor();

        }
    }
}
