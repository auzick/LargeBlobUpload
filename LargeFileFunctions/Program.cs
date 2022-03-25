using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LargeFileFunctions
{
    public class Program
    {
        private static AzureFunctionSettings azureFunctionSettings = new AzureFunctionSettings();

        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                
                .ConfigureServices((context, services) =>
                {
                    // Binding settings from local.settings.json
                    services.AddOptions<AzureFunctionSettings>().Configure<IConfiguration>((settings, configuration) =>
                    {
                        configuration.GetSection("Values").Bind(settings);
                    });

                    // Add our global configuration instance
                    services.AddSingleton(options =>
                    {
                        var configuration = context.Configuration;
                        azureFunctionSettings = new AzureFunctionSettings();
                        configuration.Bind(azureFunctionSettings);
                        return configuration;
                    });

                    //Add our configuration class
                    services.AddSingleton(options => azureFunctionSettings);

                    services.AddHttpClient();
                })
                
                .Build();

            host.Run();
        }
    }
}