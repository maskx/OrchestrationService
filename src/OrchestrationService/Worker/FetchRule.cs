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
            StringBuilder sb = new StringBuilder();
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
                            options.CommunicationTableName);
                    limitaitonWhere.Add(string.Format("T{0}.Locked<{1}", index, limitation.Concurrency));
                    index++;
                }
                sb.Append(string.Format(ruleTemplate, limitationQuery, string.Join(" and ", limitaitonWhere), options.CommunicationTableName));
                others.Add($"({rule.Where})");
            }
            sb.Append(string.Format(otherTemplate, options.MaxConcurrencyRequest, string.Join(" and ", others), options.CommunicationTableName));
            return sb.ToString();
        }

        // {0} where
        // {1} group
        // {2} limit index
        // {3} on
        // {4} Communication table name
        private const string limitationTemplate = @"
inner join (
        select
            COUNT(case when [status]='Locked' then 1 else null end) as Locked,
	        {1}
        from {4}
        where    {0}
        group by {1}
    ) as T{2}
    on {3}";

        // {0} limitation query
        // {1} limitation where
        // {2} Communication table name
        private const string ruleTemplate = @"
update top(1) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.*
FROM {2} AS T {0}
where [status]=N'Pending' and {1}

if @RequestId is not null
begin
    return
end
";

        // {0} Concurrency of others
        // {1} limitation where
        // [2} Communication table name
        private const string otherTemplate = @"
update top({0}) T
set @RequestId=T.RequestId=newid(),T.[Status]=N'Locked'
output INSERTED.*
FROM {2} AS T
where [status]=N'Pending' and not ({1})
";
    }
}