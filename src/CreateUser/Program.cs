using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Graph;

using Azure.Identity;

namespace CreateUser
{

    class Program
    {
        
        static async Task Main(string[] args)
        {

            var configuration = GetConfiguration(args);

            var clientId = configuration.GetSection("ClientId").Value;
            var clientSecret = configuration.GetSection("ClientSecret").Value;
            var tenantId = configuration.GetSection("TenantId").Value;

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

            var graphServiceClient = new GraphServiceClient(clientSecretCredential);

            var messages = await GetMessages(configuration, graphServiceClient);

            Console.WriteLine(messages.Count);

        }

        private async static Task<List<Message>> GetMessages(IConfiguration configuration, GraphServiceClient client)
        {
            var mailbox = configuration.GetSection("MailboxName").Value;

            var filteredUsers = await client.Users.Request().Filter($"UserPrincipalName eq '{mailbox}'").GetAsync();

            // If we want to list all users for the debug reasons
            // var filteredUsers = await graphServiceClient.Users.Request().GetAsync();

            var fisrtUser = filteredUsers.First();

            // We left next lines commented to have evidence that the 'ProxyAddresses' field is read-only in the current release of Graph API
            //fisrtUser.ProxyAddresses = new List<string>() { $"smtp:alias-of-{mailbox}" };
            //var user = await client.Users[fisrtUser.Id].Request().UpdateAsync(fisrtUser);

            var folder = configuration.GetSection("AliasName").Value; // "tenant1-alias1"

            var existingFolders = await client.Users[fisrtUser.Id].MailFolders.Request().GetAsync();

            existingFolders = await client.Users[fisrtUser.Id].MailFolders.Request().Filter($"displayName eq '{folder}'").GetAsync();

            // Here we try to get a specific raw message header and we get an empty response
            //var messagesInFolder = await client.Users[fisrtUser.Id].MailFolders[existingFolder.First().Id].Messages.Request()
            //    .Select("singleValueExtendedProperties")
            //    .Expand("singleValueExtendedProperties($filter=id eq 'String 0x007D')").GetAsync();

            // Here we try to get raw message header with no luck
            //var messagesInFolder = await client.Users[fisrtUser.Id].MailFolders[existingFolders.First().Id].Messages.Request()
            //    .Select("internetMessageHeaders").GetAsync();

            if (existingFolders.Count > 0)
            {
                var messagesInFolder = await client.Users[fisrtUser.Id].MailFolders[existingFolders.First().Id].Messages.Request().GetAsync();
                return messagesInFolder.ToList();
            }

            return new List<Message>();
        }

        private static IConfiguration GetConfiguration(string[] args)
        {
            var environmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");

            if (string.IsNullOrEmpty(environmentName))
                environmentName = "Development";

            environmentName = FindNextValue(args, "EnvironmentName", defaultValue: environmentName);

            return new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Program>()
                    .Build();
        }

        private static string FindNextValue(string[] args, string key, string defaultValue = null)
        {
            if (args == null) return defaultValue;

            var index = Array.IndexOf(args, key);

            if (index != -1)
            {
                return args[index + 1];
            }
            else return defaultValue;
        }

    }

}