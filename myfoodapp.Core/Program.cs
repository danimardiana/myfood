using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using myfoodapp.Core.Common;
using myfoodapp.Core.Business;

namespace myfoodapp.Core
{
    public class Program
    {
        private static ServiceProvider serviceProvider;
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();  
        }
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://*:5000");
                    webBuilder
                    .ConfigureLogging((ctx, builder) =>
                    {
                        builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                        builder.AddFile(o => o.RootPath = ctx.HostingEnvironment.ContentRootPath);
                    })
                    .UseStartup<Startup>(); 

                    var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build(); 

                    var services = new ServiceCollection();

                    services.AddLogging(builder =>
                    {
                        builder.AddConfiguration(configuration.GetSection("Logging"));
                        builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                    });

                    serviceProvider = services.BuildServiceProvider();

                    ILogger<Program> logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

                    LogManager.GetInstance.SetInstance(logger);

                    var lg = LogManager.GetInstance;                       
                    lg.AppendLog(Log.CreateLog("Log Manager Engaged", LogType.Information));

                    //var bw = new BackgroundManager(); 
                    var bw = MeasureBackgroundTask.GetInstance;
                    bw.Run();
  
                });
    }
}
