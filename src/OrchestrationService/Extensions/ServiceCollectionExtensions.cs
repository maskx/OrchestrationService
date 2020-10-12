using DurableTask.Core;
using maskx.DurableTask.SQLServer;
using maskx.DurableTask.SQLServer.Tracking;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UsingOrchestration(this IServiceCollection services, Func<IServiceProvider, SqlServerConfiguration> configOptions)
        {
            return UsingOrchestration(services, (sp) =>
            {
                var options = configOptions(sp);
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
                    configuration.CommunicationWorkerOptions.IdelMilliseconds = options.CommunicationWorkerOptions.IdelMilliseconds;
                    configuration.CommunicationWorkerOptions.MaxConcurrencyRequest = options.CommunicationWorkerOptions.MaxConcurrencyRequest;
                    configuration.CommunicationWorkerOptions.RuleFields = options.CommunicationWorkerOptions.RuleFields;
                }
                return configuration;
            });
        }
        public static IServiceCollection UsingOrchestration(this IServiceCollection services, Func<IServiceProvider, OrchestrationServiceConfiguration> configureOptions)
        {
            services.AddHttpClient();
            services.AddSingleton((sp) =>
            {
                return configureOptions(sp);

            });
            services.AddSingleton((sp) =>
            {
                var option = sp.GetService<OrchestrationServiceConfiguration>();
                return option.OrchestrationService;
            });
            services.AddSingleton((sp) =>
            {
                var option = sp.GetService<OrchestrationServiceConfiguration>();
                return option.OrchestrationServiceClient;
            });

            #region OrchestrationWorker
            services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
            {
                var option = sp.GetService<OrchestrationServiceConfiguration>();
                if (option.GetOrchestrationCreatorFactory == null)
                {
                    OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
                    orchestrationCreatorFactory.RegistCreator("NameVersionDICreator", typeof(NameVersionDICreator<TaskOrchestration>));
                    return orchestrationCreatorFactory;
                }
                else
                    return option.GetOrchestrationCreatorFactory(sp);

            });
            services.AddSingleton((sp) =>
            {
                var option = sp.GetService<OrchestrationServiceConfiguration>();
                var opt = new Worker.OrchestrationWorkerOptions();
                if (option.OrchestrationWorkerOptions == null)
                {
                    opt.GetBuildInOrchestrators = (sp) =>
                    {
                        return new List<(string Name, string Version, Type Type)>(); ;
                    };
                    opt.GetBuildInTaskActivities = (sp) =>
                    {
                        return new List<(string Name, string Version, Type Type)>();
                    };
                }
                else
                {
                    opt.IncludeDetails = option.OrchestrationWorkerOptions.IncludeDetails;
                    opt.AutoCreate = option.OrchestrationWorkerOptions.AutoCreate;
                    opt.FetchJobCount = option.OrchestrationWorkerOptions.FetchJobCount;
                    opt.GetBuildInOrchestrators = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> orc;
                        if (option.OrchestrationWorkerOptions.GetBuildInOrchestrators == null)
                            orc = new List<(string Name, string Version, Type Type)>();
                        else
                            orc = option.OrchestrationWorkerOptions.GetBuildInOrchestrators(sp);
                        orc.Add((typeof(AsyncRequestOrchestration).FullName, "", typeof(AsyncRequestOrchestration)));
                        return orc;
                    };
                    opt.GetBuildInTaskActivities = (sp) =>
                    {
                        IList<(string Name, string Version, Type Type)> act;
                        if (option.OrchestrationWorkerOptions.GetBuildInTaskActivities == null)
                            act = new List<(string Name, string Version, Type Type)>();
                        else
                            act = option.OrchestrationWorkerOptions.GetBuildInTaskActivities(sp);
                        act.Add((typeof(AsyncRequestActivity).FullName, "", typeof(AsyncRequestActivity)));
                        act.Add((typeof(HttpRequestActivity).FullName, "", typeof(HttpRequestActivity)));
                        return act;
                    };
                    opt.GetBuildInTaskActivitiesFromInterface = option.OrchestrationWorkerOptions.GetBuildInTaskActivitiesFromInterface;
                }
                return Options.Create(opt);
            });
            services.AddSingleton<OrchestrationWorker>();
            services.AddSingleton<IHostedService>(p =>
            {
                return p.GetService<OrchestrationWorker>();
            });
            #endregion

            services.AddSingleton((sp) =>
            {
                var option = sp.GetService<OrchestrationServiceConfiguration>();

                if (option.CommunicationWorkerOptions != null)
                {
                    var opt = new Worker.CommunicationWorkerOptions
                    {
                        AutoCreate = option.CommunicationWorkerOptions.AutoCreate,
                        ConnectionString = option.CommunicationWorkerOptions.ConnectionString,
                        RuleFields = option.CommunicationWorkerOptions.RuleFields,
                        HubName = option.CommunicationWorkerOptions.HubName,
                        IdelMilliseconds = option.CommunicationWorkerOptions.IdelMilliseconds,
                        MaxConcurrencyRequest = option.CommunicationWorkerOptions.MaxConcurrencyRequest,
                        SchemaName = option.CommunicationWorkerOptions.SchemaName
                    };
                    return Options.Create<Worker.CommunicationWorkerOptions>(opt);
                }
                return null;
            });
            services.AddSingleton<CommunicationWorker>();
            services.AddSingleton<IHostedService>(p =>
            {
                return p.GetService<CommunicationWorker>();
            });

            services.AddSingleton<OrchestrationWorkerClient>();
            return services;
        }
    }
}