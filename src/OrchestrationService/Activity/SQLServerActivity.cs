using DurableTask.Core;
using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Activity
{
    public class SQLServerActivity<T> : TaskActivity<SQLServerInput, T>
    {
        private OrchestrationWorkerOptions options;

        public SQLServerActivity(OrchestrationWorkerOptions options)
        {
            this.options = options;
        }

        protected override T Execute(TaskContext context, SQLServerInput input)
        {
            throw new NotImplementedException();
        }
    }
}