using DurableTask.Core;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

namespace maskx.OrchestrationService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingCommunicationWorker(this IServiceCollection services,
            Func<IServiceProvider, CommunicationWorkerOptions> config = null)
        {
            if (config != null)
                services.TryAddSingleton(sp => Options.Create(config(sp)));
            services.TryAddSingleton<CommunicationWorker>();
            services.AddSingleton<IHostedService>(p =>
            {
                return p.GetService<CommunicationWorker>();
            });
            return services;
        }
        public static IServiceCollection UsingCommunicationWorkerClient(this IServiceCollection services,
            Func<IServiceProvider, CommunicationWorkerOptions> configOptions)
        {
            services.TryAddSingleton(sp => Options.Create(configOptions(sp)));
            services.TryAddSingleton<CommunicationWorkerClient>();
            return services;
        }
        public static IServiceCollection UsingOrchestrationWorker(this IServiceCollection services,
            Func<IServiceProvider, OrchestrationWorkerOptions> config = null,
            Func<IServiceProvider, IOrchestrationCreatorFactory> getOrchestrationCreatorFactory = null)
        {
            if (config != null)
                services.AddSingleton(sp => Options.Create(config(sp)));
            services.TryAddSingleton((sp) =>
            {
                if (getOrchestrationCreatorFactory == null)
                {
                    OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                    orchestrationCreatorFactory.RegistCreator("NameVersionDICreator", typeof(NameVersionDICreator<TaskOrchestration>));
                    return orchestrationCreatorFactory;
                }
                else
                    return getOrchestrationCreatorFactory(sp);

            });
            services.TryAddSingleton<OrchestrationWorker>();
            services.AddSingleton<IHostedService>(p =>
            {
                return p.GetService<OrchestrationWorker>();
            });
            return services;
        }

        public static IServiceCollection UsingSQLServerOrchestration(this IServiceCollection services, SqlServerOrchestrationConfiguration configuration)
        {
            var sqlServerStore = new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
            {
                HubName = configuration.HubName,
                SchemaName = configuration.SchemaName,
                ConnectionString = configuration.ConnectionString
            });
            var orchestrationService = new SQLServerOrchestrationService(
                       configuration.ConnectionString,
                       configuration.HubName,
                       sqlServerStore,
                       configuration);
            services.AddSingleton<IOrchestrationService>(orchestrationService);
            services.AddSingleton<IOrchestrationServiceClient>(orchestrationService);
            return services;
        }
    }
}