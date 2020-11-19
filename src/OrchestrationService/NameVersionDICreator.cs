using System;

namespace maskx.OrchestrationService
{
    public class NameVersionDICreator<T> : DICreator<T>
    {
        public NameVersionDICreator(IServiceProvider serviceProvider, string name, string version, Type type) : base(serviceProvider, type)
        {
            this.Name = name;
            this.Version = version;
        }
    }
}