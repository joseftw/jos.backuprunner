using JOS.BackupRunner.Infrastructure.NAS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JOS.BackupRunner.Infrastructure
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static void AddInfrastructureFeature(this IServiceCollection services)
        {
            services.AddOptions<SynologyNasOptions>().BindConfiguration("infrastructure:synology").ValidateDataAnnotations();
            services.AddSingleton<SynologyNasOptions>(x => x.GetRequiredService<IOptions<SynologyNasOptions>>().Value);
            services.AddHttpClient<SynologyNasHttpClient>((provider, client) =>
            {
                var nasOptions = provider.GetRequiredService<SynologyNasOptions>();
                client.BaseAddress = nasOptions.BaseAddress;
            });
        }
    }
}