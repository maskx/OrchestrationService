using DurableTask.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace maskx.OrchestrationService
{
    public class DICreator<T> : ObjectCreator<T>
    {
        private IServiceProvider serviceProvider;
        private readonly Type prototype;

        public DICreator(IServiceProvider serviceProvider, Type type)
        {
            this.prototype = type;
            this.serviceProvider = serviceProvider;
            Initialize(type);
        }

        public override T Create()
        {
            var instance = ActivatorUtilities.CreateInstance(this.serviceProvider, this.prototype);
            return (T)instance;
        }

        private void Initialize(object obj)
        {
            Name = NameVersionHelper.GetDefaultName(obj);
            Version = NameVersionHelper.GetDefaultVersion(obj);
        }
    }
}