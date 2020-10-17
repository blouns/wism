using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Wism.Client.Api;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Services;
using AutoMapper;
using Serilog;
using System.IO;
using Wism.Client.AsciiUi;
using Wism.Client.Api.Controllers;
using System.Threading.Tasks;

namespace Wism.Client.AsciiUI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console(Serilog.Events.LogEventLevel.Debug).MinimumLevel
                 .Debug().Enrich
                 .FromLogContext()
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
                    throw ex;
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Add logging
                    var configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                        .AddJsonFile("appsettings.json", false)
                        .Build();
                    services.AddSingleton<IConfigurationRoot>(configuration);
                    services.AddSingleton(LoggerFactory.Create(builder =>
                    {
                        builder.AddSerilog(dispose: true);
                    }));                 
                    services.AddLogging();                    

                    // Add database
                    services.AddScoped<IWismClientRepository, WismClientSqliteRepository>();
                    services.AddDbContextPool<WismClientDbContext>(options =>
                    {
                        options.UseSqlite(configuration.GetConnectionString("WismClientDb"));
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

    /*
    class Program
    {
        private static IConfigurationRoot _configuration;
        private static WismClientDbContext _wismDbContext;
        private static IWismClientRepository _wismClientRepository;

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console(Serilog.Events.LogEventLevel.Debug).MinimumLevel
                 .Debug().Enrich
                 .FromLogContext()
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
            Log.Information("Creating service collection");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Log.Information("Building service provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            _wismDbContext = serviceProvider.GetService<WismClientDbContext>();
            _wismClientRepository = serviceProvider.GetService<IWismClientRepository>();
            Console.WriteLine(_configuration.GetConnectionString("WismClientDb"));

            try
            {
                Log.Information("Starting service");
                await serviceProvider.GetService<Game>().Run();
                Log.Information("Ending service");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error running service");
                throw ex;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        //private static IHostBuilder CreateHostBuilder(string[] args)
        //{
        //    // Host.CreateDefaultBuilder(args)
        //    // TODO: Need to find new impl of this 
        //    // https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/dbcontext-creation
        //}

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(LoggerFactory.Create(builder =>
            {
                builder.AddSerilog(dispose: true);
            }));
            services.AddLogging();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            services.AddSingleton<IConfigurationRoot>(_configuration);

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            services.AddScoped<IWismClientRepository, WismClientSqliteRepository>();

            services.AddDbContextPool<WismClientDbContext>(options =>
            {
                options.UseSqlite(_configuration.GetConnectionString("WismClientDb"));
            });

            services.AddTransient<Game>();
        }
    }*/
}
