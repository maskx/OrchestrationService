using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace maskx.OrchestrationService
{
    public class OrchestrationCreatorFactory : IOrchestrationCreatorFactory
    {
        private readonly Dictionary<string, Type> creators = new Dictionary<string, Type>();
        private readonly IServiceProvider serviceProvider;

        public OrchestrationCreatorFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public void RegistCreator(string name, Type type)
        {
            this.creators.Add(name, type);
        }

        public T Create<T>(string name, params object[] paramas) where T : class
        {
            if (creators.TryGetValue(name, out Type v))
            {
                var instnace = ActivatorUtilities.CreateInstance(this.serviceProvider, v, paramas);
                return (T)instnace;
            }
            return default;
        }
    }
}