using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using AddMailAliasService.HostedServices;

namespace AddMailAliasService
{

    class Program
    {

        static async Task Main(string[] args)
        {

            var builder = Host.CreateDefaultBuilder()
                            .ConfigureHostConfiguration((config) =>
                            {
                                config.AddCommandLine(args);
                            })
                            .ConfigureAppConfiguration((hostContext, config) =>
                            {
                                // This is to prevent adding secrets when run in a container. Environmental variables are used instead.
                                if (hostContext.HostingEnvironment.IsDevelopment())
                                {
                                    config.AddUserSecrets<Program>();
                                }
                            })
                            .ConfigureServices((hostContext, services) =>
                            {
                                var a = hostContext.HostingEnvironment.IsDevelopment();
                                services.AddOptions<QueueListenerOptions>().Bind(hostContext.Configuration);

                                services.AddHostedService<QueueListenerService>();

                            });

            await builder.RunConsoleAsync();

        }

    }

}
