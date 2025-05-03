using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wism.Client.Agent.Services;
using Wism.Client.Agent.UI;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Data;

namespace Wism.Client.Agent;

public class Program
{
    public static int Main(string[] args)
    {
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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public static async Task MainAsync(string[] args)
#pragma warning restore CS1998
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
        Task[] tasks =
        {
            // Start the host
            host.RunAsync(),

            // Start the UI
            scope.ServiceProvider.GetService<GameBase>().RunAsync()
        };
        Task.WaitAny(tasks);
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                // Add configuration
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", false)
                    .Build();
                services.AddSingleton(configuration);

                // Add database
                services.AddSingleton<IWismClientRepository, WismClientInMemoryRepository>(provider =>
                    new WismClientInMemoryRepository(new SortedList<int, Command>())
                );

                services.AddSingleton<IWismLoggerFactory, WismLoggerFactory>();

                // Add controllers
                services.AddSingleton(provider =>
                    new ControllerProvider
                    {
                        ArmyController = new ArmyController(
                            provider.GetService<IWismLoggerFactory>()),
                        CommandController = new CommandController(
                            provider.GetService<IWismLoggerFactory>(),
                            provider.GetService<IWismClientRepository>()),
                        GameController = new GameController(
                            provider.GetService<IWismLoggerFactory>()),
                        CityController = new CityController(
                            provider.GetService<IWismLoggerFactory>()),
                        LocationController = new LocationController(
                            provider.GetService<IWismLoggerFactory>()),
                        HeroController = new HeroController(
                            provider.GetService<IWismLoggerFactory>()),
                        PlayerController = new PlayerController(
                            provider.GetService<IWismLoggerFactory>())
                    });

                // Add command agent
                services.AddSingleton<IHostedService>(provider =>
                    new WismAgent(
                        provider.GetService<IWismLoggerFactory>(),
                        provider.GetService<ControllerProvider>()));

                // Add view
                services.AddTransient<GameBase, AsciiGame>(provider =>
                    new AsciiGame(
                        provider.GetService<IWismLoggerFactory>(),
                        provider.GetService<ControllerProvider>()));
            });
    }
}