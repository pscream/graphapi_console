using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Azure.Messaging.ServiceBus;

using AddMailAliasService.Messages;
using AddMailAliasService.Operations;

namespace AddMailAliasService.HostedServices
{
    internal class QueueListenerService : IHostedService
    {

        private Guid _serviceId;
        private Task _executingTask;
        private readonly CancellationTokenSource _stoppingCts =
                                                       new CancellationTokenSource();

        private readonly ILoggerFactory _loggerFactory;
        private readonly QueueListenerOptions _options;

        private readonly string _connectionString = string.Empty;
        private readonly string _queueName = string.Empty;
        private readonly string _adminUsername = string.Empty;
        private readonly string _adminPassword = string.Empty;
        private readonly string _azurePlanName = string.Empty;
        private readonly string _tenantDomain = string.Empty;

        public QueueListenerService(ILoggerFactory loggerFactory, IOptions<QueueListenerOptions> options)
        {
            _loggerFactory = loggerFactory;
            _serviceId = Guid.NewGuid();
            _options = options.Value;
            _connectionString = options.Value.ServiceBus.ConnectionString;
            _queueName = options.Value.ServiceBus.QueueName;
            _adminUsername = options.Value.AdminUPN;
            _adminPassword = options.Value.AdminPWD;
            _azurePlanName = options.Value.AzurePlanName;
            _tenantDomain = options.Value.TenantDomain;
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var serviceStringId = _serviceId.ToString();

            //var adminClient = new ServiceBusAdministrationClient(_options.ConnectionString);
            //CreateSubscriptionOptions options = new CreateSubscriptionOptions(_queueName, serviceStringId);

            //var subscription = await adminClient.CreateSubscriptionAsync(options, stoppingToken);

            var client = new ServiceBusClient(_connectionString);

            // create a processor that we can use to process the messages
            var processor = client.CreateProcessor(_queueName);

            try
            {

                processor.ProcessMessageAsync += MessageHandler;
                processor.ProcessErrorAsync += ErrorHandler;

                await processor.StartProcessingAsync();

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);

                await processor.StopProcessingAsync(); //hangs for some reason
            }
            finally
            {
                await processor.DisposeAsync();
                await client.DisposeAsync();
            }
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            // Store the task we're executing
            _executingTask = ExecuteAsync(_stoppingCts.Token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
            }

        }

        public virtual void Dispose()
        {
            _stoppingCts.Cancel();
        }


        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                ProcessAlias action =
                    JsonSerializer.Deserialize<ProcessAlias>(args.Message.Body.ToArray());
                switch(action.Type)
                {
                    case ActionTypes.CreateTenant:
                        CreateTenant(action.TenantDisplayName, action.TenantPrincipalName, action.TenantPassword);
                        break;

                    case ActionTypes.RemoveTenant:
                        RemoveTenant(action.TenantPrincipalName);
                        break;

                    case ActionTypes.CreateAlias:
                        CreateAlias(action.TenantPrincipalName, action.TenantPassword, action.NewAliasName);
                        break;

                    case ActionTypes.RenameAlias:
                        RenameAlias(action.TenantPrincipalName, action.TenantPassword, action.CurrentAliasName, action.NewAliasName);
                        break;

                    case ActionTypes.RemoveAlias:
                        RemoveAlias(action.TenantPrincipalName, action.TenantPassword, action.CurrentAliasName);
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.ToString());
            }
            finally
            {
                await args.CompleteMessageAsync(args.Message);
            }
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine("Error: {0}", args.Exception.ToString());
            return Task.CompletedTask;
        }

        private void CreateTenant(string displayName, string principalName, string password)
        {

            var operation = new TenantOperation(_loggerFactory.CreateLogger<TenantOperation>(), _adminUsername, _adminPassword);

            operation.CreateADUser(null, displayName, principalName, password, _azurePlanName);

        }

        private void RemoveTenant(string principalName)
        {

            var operation = new TenantOperation(_loggerFactory.CreateLogger<TenantOperation>(), _adminUsername, _adminPassword);

            operation.RemoveADUser(null, principalName);

        }

        private void CreateAlias(string principalName, string password, string aliasName)
        {

            var operation = new AliasOperation(_loggerFactory.CreateLogger<AliasOperation>(), _adminUsername, _adminPassword, _tenantDomain);

            operation.CreateMailAlias(null, principalName, password, aliasName);

        }

        private void RenameAlias(string principalName, string password, string currentAliasName, string newAliasName)
        {

            var operation = new AliasOperation(_loggerFactory.CreateLogger<AliasOperation>(), _adminUsername, _adminPassword, _tenantDomain);

            operation.RenameMailAlias(null, principalName, password, currentAliasName, newAliasName);

        }

        private void RemoveAlias(string principalName, string password, string aliasName)
        {

            var operation = new AliasOperation(_loggerFactory.CreateLogger<AliasOperation>(), _adminUsername, _adminPassword, _tenantDomain);

            operation.RemoveMailAlias(null, principalName, password, aliasName);

        }

    }

}
