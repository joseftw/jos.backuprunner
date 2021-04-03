using System.Threading.Tasks;
using JOS.BackupRunner.Backup;
using JOS.BackupRunner.Infrastructure;
using JOS.BackupRunner.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace JOS.BackupRunner
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddCommandLine(args).Build();
            var environment = GetEnvironment(configuration);

            var builder = new HostBuilder()
                .UseEnvironment(environment)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddConfiguration(configuration);
                    config.AddJsonFile("appsettings.json");
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment}.json", optional: true);
                    config.AddJsonFile("appsettings.Local.json", optional: true);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);

                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        config.AddUserSecrets<Program>();
                    }
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    _ = new LoggerConfigurator(hostingContext.Configuration, hostingContext.HostingEnvironment).Configure();
                    logging.AddSerilog(dispose: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddInfrastructureFeature();
                    services.AddBackupFeature();
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateOnBuild = true;
                    options.ValidateScopes = true;
                });
            
            var host = builder.Build();
            var hostApplicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

            var backupRunner = host.Services.GetRequiredService<Backup.BackupRunner>();
            await backupRunner.Execute(hostApplicationLifetime.ApplicationStopping);
        }

        private static string GetEnvironment(IConfiguration configuration)
        {
            var environment = configuration.GetValue<string>("environment");

            if (string.IsNullOrWhiteSpace(environment))
            {
                environment = Environments.Development;
            }

            return environment;
        }
    }
}
