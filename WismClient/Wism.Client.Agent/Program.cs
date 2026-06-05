using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wism.Client.Agent.CommandProcessors.Factories;
using Wism.Client.Agent.Services;
using Wism.Client.Agent.UI;
using Wism.Client.Api.Telemetry;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Data;
using Wism.Client.Agent.Telemetry;
using Wism.Client.Core.Telemetry;
using Wism.Client.Agent.CommandProcessors;
using Wism.Client.Agent.CommandProcessors.SearchProcessors;
using Wism.Companion.Shared.Events;

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
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
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

                // Optional: For companion app
                services.AddSingleton(CreateTelemetryContext(args));
                services.AddSingleton(provider =>
                    new CommandIpcPublisher(
                        provider.GetRequiredService<IWismLoggerFactory>(),
                        provider.GetRequiredService<TelemetryContext>()));
                services.AddSingleton(provider =>
                    new MapSnapshotEmitter(
                        provider.GetRequiredService<IWismLoggerFactory>(),
                        provider.GetRequiredService<TelemetryContext>()));
                services.AddSingleton<MapSnapshotBuilder>();

                // Add command processors
                services.AddSingleton<StartTurnProcessor>();
                services.AddSingleton<RecruitHeroProcessor>();
                services.AddSingleton<HireHeroProcessor>(provider =>
                    new HireHeroProcessor(
                        provider.GetRequiredService<IWismLoggerFactory>(),
                        provider.GetService<CommandIpcPublisher>(),
                        provider.GetRequiredService<ControllerProvider>()));
                services.AddSingleton<PrepareForBattleProcessor>();
                services.AddSingleton<BattleProcessor>();
                services.AddSingleton<CompleteBattleProcessor>();
                services.AddSingleton<SearchRuinsProcessor>();
                services.AddSingleton<SearchTempleProcessor>();
                services.AddSingleton<SearchSageProcessor>();
                services.AddSingleton<SearchLibraryProcessor>();
                services.AddSingleton<StandardProcessor>();

                services.AddSingleton<ICommandProcessorFactory, CommandProcessorFactory>();

                // Add telemetry
                services.AddSingleton<IMapSnapshotBroadcaster, AsciiGameMapSnapshotBroadcaster>();

                // Add view
                services.AddTransient<GameBase>(provider =>
                    new AsciiGame(
                        provider.GetRequiredService<IWismLoggerFactory>(),
                        provider.GetRequiredService<ControllerProvider>(),
                        provider.GetRequiredService<ICommandProcessorFactory>(),
                        provider.GetRequiredService<IMapSnapshotBroadcaster>()));

                // Add command agent
                services.AddSingleton<IHostedService>(provider =>
                    new WismAgent(
                        provider.GetService<IWismLoggerFactory>(),
                        provider.GetService<ControllerProvider>()));
            });
    }

    private static TelemetryContext CreateTelemetryContext(string[] args)
    {
        var channel = ReadArg(args, "channel");
        var instanceId = Environment.ProcessId.ToString();
        return new TelemetryContext
        {
            ChannelId = string.IsNullOrWhiteSpace(channel) ? $"ascii:default:{instanceId}" : channel,
            SessionId = $"ascii:{Guid.NewGuid():N}",
            SourceKind = "Ascii",
            SourceName = "WismAgent",
            InstanceId = instanceId,
            StartedAtUtc = DateTime.UtcNow
        };
    }

    private static string ReadArg(string[] args, string name)
    {
        var prefix = name + "=";
        foreach (var arg in args)
        {
            if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return arg[prefix.Length..];
            }
        }

        return null;
    }
}
