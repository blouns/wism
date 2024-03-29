﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wism.Client.Api;
using Wism.Client.Api.Commands;
using Wism.Client.Common;
using Wism.Client.Core.Controllers;

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

                services.AddSingleton<ILoggerFactory, WismLoggerFactory>();

                // Add controllers
                services.AddSingleton(provider =>
                    new ControllerProvider
                    {
                        ArmyController = new ArmyController(
                            provider.GetService<ILoggerFactory>()),
                        CommandController = new CommandController(
                            provider.GetService<ILoggerFactory>(),
                            provider.GetService<IWismClientRepository>()),
                        GameController = new GameController(
                            provider.GetService<ILoggerFactory>()),
                        CityController = new CityController(
                            provider.GetService<ILoggerFactory>()),
                        LocationController = new LocationController(
                            provider.GetService<ILoggerFactory>()),
                        HeroController = new HeroController(
                            provider.GetService<ILoggerFactory>()),
                        PlayerController = new PlayerController(
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
    }
}