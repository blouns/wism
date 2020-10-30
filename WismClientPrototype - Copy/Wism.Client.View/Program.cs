using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using AutoMapper;
using Serilog;
using System.Threading.Tasks;
using Wism.Client.Agent;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Services;
using Wism.Client.Agent.Controllers;

namespace Wism.Client.View
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // TODO: I broke logging

            Serilog.Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console(Serilog.Events.LogEventLevel.Debug).MinimumLevel
                 .Debug().Enrich
                 .FromLogContext()
                 //.ReadFrom.Configuration()
                 .CreateLogger();

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
                SetupDatabase(scope);

                // Start and wait for services
                RunServices(host, scope);
            }
        }

        private static void SetupDatabase(IServiceScope scope)
        {
            DbContext context;
            try
            {
                context = scope.ServiceProvider.GetService<WismClientDbContext>();

                // TODO: for testing purposes, delete the database & migrate on startup so 
                // we can start with a clean slate
                context.Database.EnsureDeleted();
                context.Database.Migrate();
                context.Database.OpenConnection();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        private static void RunServices(IHost host, IServiceScope scope)
        {
            try
            {
                Log.Information("Starting services");
                Task[] tasks = new Task[]
                {
                        // Start the host
                        host.RunAsync(),

                        // Start the UI
                        scope.ServiceProvider.GetService<WismViewBase>().RunAsync()
                };
                Task.WaitAny(tasks);
                Log.Information("Ending services");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running service");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add configuration
                    var configuration = new ConfigurationBuilder()
                        //.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                        .AddJsonFile("appsettings.json", false)
                        .Build();
                    services.AddSingleton<IConfigurationRoot>(configuration);


                    // Add logging                    
                    services.AddSingleton(LoggerFactory.Create(builder =>
                    {
                        builder.AddSerilog(dispose: true);
                        builder.AddConfiguration(configuration);
                    }));
                    services.AddLogging();

                    // Add database
                    services.AddScoped<IWismClientRepository, WismClientSqliteRepository>();
                    services.AddDbContextPool<WismClientDbContext>(options =>
                    {
                        options.UseSqlServer(configuration.GetConnectionString("WismClientDb"));
                        //options.UseSqlite(configuration.GetConnectionString("WismClientDb"));
                        options.EnableSensitiveDataLogging();
                    });

                    services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                    // Add controllers
                    services.AddScoped<CommandController>(provider =>
                        new CommandController(
                                provider.GetService<ILoggerFactory>(),
                                provider.GetService<IWismClientRepository>(),
                                provider.GetService<IMapper>()));

                    // Add agent
                    services.AddSingleton<IHostedService>(provider =>
                        new WismAgent(
                            provider.GetService<ILoggerFactory>(),
                            provider.GetService<CommandController>(),
                            provider.GetService<IMapper>()));

                    // Add view
                    services.AddTransient<WismViewBase, WismAsciiView>(provider =>
                        new WismAsciiView(
                            provider.GetService<ILoggerFactory>(),
                            provider.GetService<CommandController>(),
                            provider.GetService<IMapper>()));
                });
    }
 }
