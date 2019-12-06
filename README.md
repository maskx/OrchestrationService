# OrchestrationService

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

### Add FetchRule

some system have concurrency request limitation, you can add FetchRule to control the concurrency request to the system

```CSharp
var options = new CommunicationWorkerOptions();
options.GetFetchRules = () =>
{
    var r1 = new FetchRule()
    {
        What = new Dictionary<string, string>() { { "Processor", "MockCommunicationProcessor" } },
    };
    r1.Limitions.Add(new Limitation()
    {
        Concurrency = 1,
        Scope = new List<string>()
        {
            "RequestOperation"
        }
    });
    r1.Limitions.Add(new Limitation
    {
        Concurrency = 5,
        Scope = new List<string>()
        {
            "RequestTo"
        }
    });
    List<FetchRule> fetchRules = new List<FetchRule>();
    fetchRules.Add(r1);
    return fetchRules;
};
```

### add yourself rule field

1. Add your rule field to Communication table in database
2. add your rule field in CommunicationWorkerOptions

```CSharp
var options = new CommunicationWorkerOptions();
options.GetFetchRules = () =>
{
    var r1 = new FetchRule()
    {
        What = new Dictionary<string, string>() { { "Processor", "MockCommunicationProcessor" } },
    };
    r1.Limitions.Add(new Limitation()
    {
        Concurrency = 1,
        Scope = new List<string>()
        {
            "SubscriptionId"
        }
    });
    r1.Limitions.Add(new Limitation
    {
        Concurrency = 5,
        Scope = new List<string>()
        {
            "ManagementUnit"
        }
    });
    List<FetchRule> fetchRules = new List<FetchRule>();
    fetchRules.Add(r1);
    return fetchRules;
};
options.RuleFields.Add("SubscriptionId");
options.RuleFields.Add("ManagementUnit");
```

## TaskOrchestration

### [AsyncRequestOrchestration](#AsyncRequestOrchestration)

## TaskActivity

### HttpRequestActivity

#### HttpRequestInput

### SQLServerActivity


