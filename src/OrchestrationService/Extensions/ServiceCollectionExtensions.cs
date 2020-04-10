using DurableTask.Core;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingOrchestration(this IServiceCollection services, SqlServerConfiguration options)
        {
            OrchestrationServiceConfiguration configuration = new OrchestrationServiceConfiguration();
            var sqlServerStore = new SqlServerInstanceStore(new SqlServerInstanceStoreSettings()
            {
                HubName = options.HubName,
                SchemaName = options.SchemaName,
                ConnectionString = options.ConnectionString
            });
            var orchestrationService = new SQLServerOrchestrationService(
                       options.ConnectionString,
                       options.HubName,
                       sqlServerStore,
                       options.OrchestrationServiceSettings);
            configuration.OrchestrationService = orchestrationService;
            configuration.OrchestrationServiceClient = orchestrationService;
            configuration.OrchestrationWorkerOptions = new Worker.OrchestrationWorkerOptions()
            {
                AutoCreate = options.AutoCreate
            };
            if (options.OrchestrationWorkerOptions != null)
            {
                configuration.OrchestrationWorkerOptions.IncludeDetails = options.OrchestrationWorkerOptions.IncludeDetails;
                configuration.OrchestrationWorkerOptions.FetchJobCount = options.OrchestrationWorkerOptions.FetchJobCount;
                configuration.OrchestrationWorkerOptions.GetBuildInOrchestrators = options.OrchestrationWorkerOptions.GetBuildInOrchestrators;
                configuration.OrchestrationWorkerOptions.GetBuildInTaskActivities = options.OrchestrationWorkerOptions.GetBuildInTaskActivities;
                configuration.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface = options.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
            }
            configuration.CommunicationWorkerOptions = new Worker.CommunicationWorkerOptions()
            {
                ConnectionString = options.ConnectionString,
                HubName = options.HubName,
                SchemaName = options.SchemaName,
                AutoCreate = options.AutoCreate
            };
            if (options.CommunicationWorkerOptions != null)
            {
                configuration.CommunicationWorkerOptions.GetFetchRules = options.CommunicationWorkerOptions.GetFetchRules;
                configuration.CommunicationWorkerOptions.IdelMilliseconds = options.CommunicationWorkerOptions.IdelMilliseconds;
                configuration.CommunicationWorkerOptions.MaxConcurrencyRequest = options.CommunicationWorkerOptions.MaxConcurrencyRequest;
                configuration.CommunicationWorkerOptions.RuleFields = options.CommunicationWorkerOptions.RuleFields;
            }
            return UsingOrchestration(services, configuration);
        }

        public static IServiceCollection UsingOrchestration(this IServiceCollection services, OrchestrationServiceConfiguration options)
        {
            services.AddSingleton(options.OrchestrationService);
            services.AddSingleton(options.OrchestrationServiceClient);

            #region OrchestrationWorker

            if (options.GetOrchestrationCreatorFactory == null)
            {
                services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
                {
                    OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                    orchestrationCreatorFactory.RegistCreator("NameVersionDICreator", typeof(NameVersionDICreator<TaskOrchestration>));
                    return orchestrationCreatorFactory;
                });
            }
            else
            {
                services.AddSingleton((sp) => options.GetOrchestrationCreatorFactory(sp));
            }

            if (options.OrchestrationWorkerOptions == null)
            {
                services.Configure<Worker.OrchestrationWorkerOptions>((opt) =>
                {
                    opt.AutoCreate = options.OrchestrationWorkerOptions.AutoCreate;
                    opt.FetchJobCount = options.OrchestrationWorkerOptions.FetchJobCount;
                    opt.GetBuildInOrchestrators = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> orc;
                        if (options.OrchestrationWorkerOptions == null || options.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                            orc = new List<(string Name, string Version, Type Type)>();
                        else
                            orc = options.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                        return orc;
                    };
                    opt.GetBuildInTaskActivities = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> act;
                        if (options.OrchestrationWorkerOptions == null || options.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                            act = new List<(string Name, string Version, Type Type)>();
                        else
                            act = options.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                        return act;
                    };
                    opt.GetBuildInTaskActivitiesFromInterface = options.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
                });
            }
            else
            {
                services.Configure<Worker.OrchestrationWorkerOptions>(opt =>
                {
                    opt.IncludeDetails = options.OrchestrationWorkerOptions.IncludeDetails;
                    opt.AutoCreate = options.OrchestrationWorkerOptions.AutoCreate;
                    opt.FetchJobCount = options.OrchestrationWorkerOptions.FetchJobCount;
                    opt.GetBuildInOrchestrators = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> orc;
                        if (options.OrchestrationWorkerOptions == null || options.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                            orc = new List<(string Name, string Version, Type Type)>();
                        else
                            orc = options.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                        orc.Add((typeof(AsyncRequestOrchestration).FullName, "", typeof(AsyncRequestOrchestration)));
                        return orc;
                    };
                    opt.GetBuildInTaskActivities = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> act;
                        if (options.OrchestrationWorkerOptions == null || options.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                            act = new List<(string Name, string Version, Type Type)>();
                        else
                            act = options.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                        act.Add((typeof(AsyncRequestActivity).FullName, "", typeof(AsyncRequestActivity)));
                        return act;
                    };
                    opt.GetBuildInTaskActivitiesFromInterface = options.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
                });
            }

            services.AddSingleton<OrchestrationWorker>();
            services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());

            #endregion OrchestrationWorker

            #region CommunicationWorker

            if (options.CommunicationWorkerOptions != null)
            {
                services.Configure<Worker.CommunicationWorkerOptions>((opt) =>
                {
                    opt.AutoCreate = options.CommunicationWorkerOptions.AutoCreate;
                    opt.ConnectionString = options.CommunicationWorkerOptions.ConnectionString;
                    opt.RuleFields = options.CommunicationWorkerOptions.RuleFields;
                    opt.GetFetchRules = options.CommunicationWorkerOptions.GetFetchRules;
                    opt.HubName = options.CommunicationWorkerOptions.HubName;
                    opt.IdelMilliseconds = options.CommunicationWorkerOptions.IdelMilliseconds;
                    opt.MaxConcurrencyRequest = options.CommunicationWorkerOptions.MaxConcurrencyRequest;
                    opt.SchemaName = options.CommunicationWorkerOptions.SchemaName;
                });
                services.AddSingleton<CommunicationWorker>();
                services.AddSingleton<IHostedService>(p => p.GetService<CommunicationWorker>());
            }

            #endregion CommunicationWorker

            services.AddSingleton<OrchestrationWorkerClient>();
            return services;
        }
    }
}