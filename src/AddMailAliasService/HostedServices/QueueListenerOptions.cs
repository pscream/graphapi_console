namespace AddMailAliasService.HostedServices
{

    internal class QueueListenerOptions
    {

        public string AdminUPN { get; set; }

        public string AdminPWD { get; set; }

        public ServiceBusOptions ServiceBus { get; set; }

        public string AzurePlanName { get; set; }

        public string TenantDomain { get; set; }

    }

}
