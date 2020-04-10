using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationService.Worker
{
    public class FetchRule
    {
        /// <summary>
        /// 需要限制并发请求的内容，如ServicType,RequestMethod，Operation
        /// </summary>
        public Dictionary<string, string> What { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 限制并发请求的范围，如Subscription、ManagementUnit
        /// </summary>
        public List<Limitation> Limitions { get; set; } = new List<Limitation>();

        private string where;

        public string Where
        {
            get
            {
                if (string.IsNullOrEmpty(where))
                {
                    List<string> s = new List<string>();
                    foreach (var item in What)
                    {
                        s.Add($"{item.Key}=N'{item.Value}'");
                    }
                    if (s.Count > 0)
                        where = string.Join(" And ", s);
                }
                return where;
            }
        }

        public static string BuildFetchCommand(List<FetchRule> fetchRules, CommunicationWorkerOptions options)
        {
            StringBuilder sb = new StringBuilder("declare @Count int=0;");
            List<string> others = new List<string>();
            int index = 0;
            foreach (var rule in fetchRules)
            {
                string limitationQuery = string.Empty;
                List<string> limitaitonWhere = new List<string>();
                foreach (var limitation in rule.Limitions)
                {
                    limitationQuery += string.Format(limitationTemplate,
                            rule.Where,
                            limitation.Group,
                            index,
                            limitation.On(index),
                            options.CommunicationTableName,
                            (int)CommunicationJob.JobStatus.Locked);
                    limitaitonWhere.Add(
                        string.Format("T{0}.Locked<{1}",
                            index,
                            limitation.Concurrency));
                    index++;
                }
                sb.Append(string.Format(ruleTemplate,
                    limitationQuery,
                    string.Join(" and ", limitaitonWhere),
                    options.CommunicationTableName,
                    (int)CommunicationJob.JobStatus.Completed,
                    (int)CommunicationJob.JobStatus.Locked,
                    options.IdelMilliseconds));
                others.Add($"({rule.Where})");
            }
            sb.Append(string.Format(otherTemplate,
                string.Join(" and ", others),
                options.CommunicationTableName,
                (int)CommunicationJob.JobStatus.Completed,
                (int)CommunicationJob.JobStatus.Locked,
                options.IdelMilliseconds));
            return sb.ToString();
        }

        // {0} where
        // {1} group
        // {2} limit index
        // {3} on
        // {4} Communication table name
        // {5} Locked status code
        private const string limitationTemplate = @"
inner join (
   select COUNT(case when [status]={5} then 1 else null end) as Locked,{1}
   from {4} where {0} group by {1}
) as T{2}  on {3}
";

        // {0} limitation query
        // {1} limitation where
        // {2} Communication table name
        // {3} Completed status code
        // {4} Locked status code
        // {5} IdelMilliseconds
        private const string ruleTemplate = @"
set @RequestId=null;
update top(1) T
set @RequestId=T.RequestId,T.[Status]={4},T.[LockedUntilUtc]=DATEADD(millisecond,{5},[LockedUntilUtc])
output INSERTED.*
FROM {2} AS T {0}
where [status]<{3} and [LockedUntilUtc]<=getutcdate() and {1}
if @RequestId is not null
begin
    set @Count=@Count+1
end
if @Count>=@MaxCount
begin
    return
end
";

        // {0} limitation where
        // {1} Communication table name
        // {2} Completed status code
        // {3} Locked status code
        // {4} IdelMilliseconds
        private const string otherTemplate = @"
update top(@MaxCount-@Count) T
set @RequestId=T.RequestId,T.[Status]={3},T.[LockedUntilUtc]=DATEADD(millisecond,{4},[LockedUntilUtc])
output INSERTED.*
FROM {1} AS T
where [status]<{2} and [LockedUntilUtc]<=getutcdate() and not ({0})
";
    }
}