# OrchestrationService

## Quick start

1. Using Orchestration Service

``` CSharp
var orchestrationTypes = new List<Type>();
orchestrationTypes.Add(typeof(<your orchestration>));
var activityTypes = new List<Type>();
activityTypes.Add(typeof(<your activity>));
var sqlConfig = new SqlServerConfiguration()
{
    ConnectionString = <sql server ConnectionString>,
    AutoCreate = true,
    OrchestrationWorkerOptions = new maskx.OrchestrationService.Extensions.OrchestrationWorkerOptions()
    {
        GetBuildInOrchestrators = () => { return orchestrationTypes; },
        GetBuildInTaskActivities = () => { return activityTypes; }
    }
};
services.UsingOrchestration(sqlConfig);
```

2. Start your orchestration

``` CSharp
var client =<IServiceProvider>.GetService<OrchestrationWorkerClient>();
client.JumpStartOrchestrationAsync(new Job
{
    InstanceId = Guid.NewGuid().ToString("N"),
    Orchestration = new OrchestrationSetting()
    {
        Creator = "DICreator",
        Uri = typeof(<your orchestration>).FullName + "_"
    },
    Input = <your orchestration input>
})
```
3. Get Orchestration result

```CSharp
var client =<IServiceProvider>.GetService<OrchestrationWorkerClient>();
var result = client.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
```

## OrchestrationWorker

### Configure

In ConfigureServices method:

1. Add OrchestrationClient and OrchestrationService,

``` CSharp
 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationClient();
                 });
                 services.AddSingleton((sp) =>
                 {
                     return CreateOrchestrationService();
                 });
```

2. Add your TaskOrchestration and TaskActivity
```CSharp
List<Type> orchestrationTypes = new List<Type>();
List<Type> activityTypes = new List<Type>();

orchestrationTypes.Add(typeof(AsyncRequestOrchestration));

activityTypes.Add(typeof(AsyncRequestActivity));
activityTypes.Add(typeof(HttpRequestActivity));

services.Configure<OrchestrationWorkerOptions>(options =>
{
    options.GetBuildInOrchestrators = () => orchestrationTypes;
    options.GetBuildInTaskActivities = () => activityTypes;
});
```
3. Add IOrchestrationCreatorFactory

* DefaultObjectCreator is DurableTask buildin Creator
* DICreator support Dependency Injection
* 
```CSharp
 services.AddSingleton<IOrchestrationCreatorFactory>((sp) =>
{
    OrchestrationCreatorFactory orchestrationCreatorFactory = new OrchestrationCreatorFactory(sp);
    orchestrationCreatorFactory.RegistCreator("DICreator", typeof(DICreator<TaskOrchestration>));
    orchestrationCreatorFactory.RegistCreator("DefaultObjectCreator", typeof(DefaultObjectCreator<TaskOrchestration>));
    return orchestrationCreatorFactory;
});
```
4. Add OrchestrationWorker
```CSharp
services.AddSingleton<OrchestrationWorker>();
services.AddSingleton<IHostedService>(p => p.GetService<OrchestrationWorker>());
```

### Send Job

#### Send job directly

``` CSharp
var orchestrationWorker = ServiceProvider.GetService<OrchestrationWorker>();
var instance = orchestrationWorker.JumpStartOrchestrationAsync(new Job()
{
    InstanceId = Guid.NewGuid().ToString("N"),
    Orchestration = new maskx.OrchestrationService.OrchestrationCreator.Orchestration()
    {
        Creator = "DICreator",
        Uri = typeof(HttpOrchestration).FullName + "_"
    },
    Input = dataConverter.Serialize(request)
}).Result;
```

#### Fetch job by a IJobProvider

``` CSharp
 services.AddSingleton<IJobProvider>(new JobProvider());
```

## CommunicationWorker

### Configure 

1. Add your CommunicationProcessor

```CSharp
services.AddSingleton<ICommunicationProcessor>(new MockCommunicationProcessor());
```

2. Configure CommunicationWorkerOptions

```CSharp
services.Configure<CommunicationWorkerOptions>((options) =>
{
    options.HubName = communicationWorkerOptions.HubName;
    options.MaxConcurrencyRequest = communicationWorkerOptions.MaxConcurrencyRequest;
    options.SchemaName = communicationWorkerOptions.SchemaName;
});
```

3. Add CommunicationWorker

```CSharp
services.AddHostedService<CommunicationWorker>();
```

### AsyncRequestOrchestration

In your TaskOrchestration, you should use AsyncRequestOrchestration send the request

```CSharp
var response = await context.CreateSubOrchestrationInstance<TaskResult>(
                  typeof(AsyncRequestOrchestration),
                  DataConverter.Deserialize<AsyncRequestInput>(input));
```

### ICommunicationProcessor

Implement yourself ICommunicationProcessor to send the request to the target system

### concurrency request limitation

some system have concurrency request limitation, you can add FetchRule to control the concurrency request to the system



## TaskOrchestration

### [AsyncRequestOrchestration](#AsyncRequestOrchestration)

## TaskActivity

### HttpRequestActivity

#### HttpRequestInput

### SQLServerActivity


