using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Core.Controllers;
using Wism.Client.Common;

namespace Wism.Client.Agent
{
    public class Program
    {
        public static int Main(string[] args)
        {
            //Serilog.Log.Logger = new LoggerConfiguration()
            //     .WriteTo.Console(Serilog.Events.LogEventLevel.Debug).MinimumLevel
            //     .Debug().Enrich
            //     .FromLogContext()
            //     .CreateLogger();

            try
            {
                MainAsync(args).Wait();
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        public static async Task MainAsync(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                // Start and wait for services
                RunServices(host, scope);
            }
        }

        private static void RunServices(IHost host, IServiceScope scope)
        {
            try
            {
                //Log.Information("Starting services");
                Task[] tasks = new Task[]
                {
                        // Start the host
                        host.RunAsync(),

                        // Start the UI
                        scope.ServiceProvider.GetService<GameBase>().RunAsync()
                };
                Task.WaitAny(tasks);
                //Log.Information("Ending services");
            }
            catch (Exception ex)
            {
                //Log.Fatal(ex, "Error running service");
            }
            finally
            {
                //Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add configuration
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json", false)
                        .Build();
                    services.AddSingleton<IConfigurationRoot>(configuration);

                    // Add database
                    services.AddSingleton<IWismClientRepository, WismClientInMemoryRepository>(provider =>
                        new WismClientInMemoryRepository(new SortedList<int, Command>())
                    );

                    services.AddSingleton<ILoggerFactory, WismLoggerFactory>();

                    // Add controllers
                    services.AddSingleton(provider =>
                        new ControllerProvider()
                        {
                            ArmyController = new ArmyController(
                                provider.GetService<ILoggerFactory>()),
                            CommandController = new CommandController(
                                provider.GetService<ILoggerFactory>(),
                                provider.GetService<IWismClientRepository>()),
                            GameController = new GameController(
                                provider.GetService<ILoggerFactory>()),
                            CityController = new CityController(
                                provider.GetService<ILoggerFactory>())
                        });

                    // Add command agent
                    services.AddSingleton<IHostedService>(provider =>
                        new WismAgent(
                            provider.GetService<ILoggerFactory>(),
                            provider.GetService<ControllerProvider>()));

                    // Add view
                    services.AddTransient<GameBase, AsciiGame>(provider =>
                        new AsciiGame(
                            provider.GetService<ILoggerFactory>(),
                            provider.GetService<ControllerProvider>()));
                });
                //.UseSerilog((hostingContext, loggerConfig) =>
                //    loggerConfig.ReadFrom.Configuration(hostingContext.Configuration)
                //);
    }
}
