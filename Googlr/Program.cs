namespace Googlr
{
    using System;
    using System.Text;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    internal class Program
    {
        // https://www.telerik.com/blogs/.net-core-background-services
        // https://garywoodfine.com/ihost-net-core-console-applications/
        public static async Task Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                // var host = new HostBuilder().Build();
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureLogging((hostContext, logging) =>
                    {
                        logging
                            .ClearProviders()
                            .SetMinimumLevel(LogLevel.Warning)
                            .AddDebug()
                            .AddConsole();
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddSingleton<Speaker>();
                        services.AddHostedService<Worker>();
                    })
                    .ConfigureHostConfiguration(config =>
                    {
                        config.AddCommandLine(args);
                    })
                    .ConfigureAppConfiguration((hostContext, config) =>
                    {
                        config.AddJsonFile("appSettings.json", optional: true, reloadOnChange: true);
                        if (hostContext.HostingEnvironment.IsDevelopment())
                        {
                            config.AddUserSecrets<Program>(true, true);
                        }
                    })
                    .UseConsoleLifetime();
                    //.Build();
                await host.RunConsoleAsync(options => options.SuppressStatusMessages = true);
            }
            catch (Exception ex)
            {
                ColorConsole.WriteLine(ex.Message.White().OnRed());
                // Console.ReadLine();
            }
        }
    }
}
