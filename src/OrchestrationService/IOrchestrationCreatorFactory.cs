using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService
{
    public interface IOrchestrationCreatorFactory
    {
        T Create<T>(string name, params object[] paramas) where T : class;

        void RegistCreator(string name, Type type);
    }
}