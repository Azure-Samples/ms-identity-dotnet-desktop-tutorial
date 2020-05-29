using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Console_Interactive_MultiTarget
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
                                                    .WithDefaultRedirectUri()
                                                    .Build();

            string[] scopes = new[] { "user.read" };

            //Setting the value of properties
            SingletonGraphServiceClient.Instance.App = app;
            SingletonGraphServiceClient.Instance.Scopes = scopes;

            //Get graph service client
            var graphClient = SingletonGraphServiceClient.GraphClient;

            // Calling the /me endpoint
            var me = await graphClient.Me.Request().GetAsync();

            // Printing the results
            Console.Write(Environment.NewLine);
            Console.Write(Environment.NewLine);
            Console.WriteLine("-------- GRAPH RESULT --------");
            Console.Write(Environment.NewLine);
            Console.WriteLine($"Id: {me.Id}");
            Console.WriteLine($"Display Name: {me.DisplayName}");
            Console.WriteLine($"Email: {me.Mail}");
        }
    }
}
