﻿using DurableTask.Core;
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
        public static IServiceCollection UsingCommunicationWorker<T>(this IServiceCollection services,
        Func<IServiceProvider, CommunicationWorkerOptions> config = null) where T : CommunicationJob, new()
        {
            if (config != null)
                services.TryAddSingleton(sp => Options.Create(config(sp)));
            services.TryAddSingleton<CommunicationWorker<T>>();
            services.AddSingleton<IHostedService>(p =>
            {
                return p.GetService<CommunicationWorker<T>>();
            });
            return services;
        }
        public static IServiceCollection UsingCommunicationWorkerClient<T>(this IServiceCollection services,
            Func<IServiceProvider, CommunicationWorkerOptions> configOptions) where T : CommunicationJob, new()
        {
            services.TryAddSingleton(sp => Options.Create(configOptions(sp)));
            services.TryAddSingleton<CommunicationWorkerClient<T>>();
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

        public static IServiceCollection UsingSQLServerOrchestration(this IServiceCollection services, Func<IServiceProvider, SqlServerOrchestrationConfiguration> config)
        {
            SQLServerOrchestrationService GetOrchestrationService(IServiceProvider sp)
            {
                var configuration = config(sp);
                var sqlServerStore = new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
                {
                    HubName = configuration.HubName,
                    SchemaName = configuration.SchemaName,
                    ConnectionString = configuration.ConnectionString
                });
                return new SQLServerOrchestrationService(
                           configuration.ConnectionString,
                           configuration.HubName,
                           sqlServerStore,
                           configuration);
            }

            services.AddSingleton<IOrchestrationService>(sp => GetOrchestrationService(sp));
            services.AddSingleton<IOrchestrationServiceClient>(sp => GetOrchestrationService(sp));
            return services;
        }
    }
}