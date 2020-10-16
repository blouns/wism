using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Wism.Client.Data.DbContexts;
using Wism.Client.Data.Services;

namespace Wism.Client.Api
{
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

        public static WismClientDbContext GetDbContext()
        {
            return _wismDbContext;
        }

        public static IWismClientRepository GetClientRepository()
        {
            return _wismClientRepository;
        }

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
    }

}
