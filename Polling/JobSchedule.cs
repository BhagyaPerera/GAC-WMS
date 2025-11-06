using System;
using System.Collections.Generic;
using System.Text;

namespace Polling
{
    public class JobSchedule
    {
        public JobSchedule(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }

        public JobSchedule(Type jobType, IEnumerable<string> cronExpressions)
        {
            JobType = jobType;
            CronExpressions = cronExpressions;
        }

        public Type JobType { get; }
        public string CronExpression { get; }
        public IEnumerable<string> CronExpressions { get; set; }
    }
}
