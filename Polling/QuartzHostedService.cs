using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Polling
{
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IEnumerable<JobSchedule> _jobSchedules;

        public QuartzHostedService(ISchedulerFactory schedulerFactory,
            IEnumerable<JobSchedule> jobSchedules,
            IJobFactory jobFactory)
        {
            _schedulerFactory = schedulerFactory;
            _jobSchedules = jobSchedules;
            _jobFactory = jobFactory;
        }

        public IScheduler Scheduler { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;

            foreach (var jobSchedule in _jobSchedules)
            {
                if (jobSchedule.CronExpressions != null)
                {
                    foreach (var cronExpression in jobSchedule.CronExpressions)
                    {
                        var job = CreateJob(jobSchedule);
                        var trigger = CreateTrigger(jobSchedule, cronExpression);
                        await Scheduler.ScheduleJob(job, trigger, cancellationToken);
                    }
                }
                else
                {
                    var job = CreateJob(jobSchedule);
                    var trigger = CreateTrigger(jobSchedule);
                    var key = job.JobType.Name;
                    switch (key)
                    {
                        case "PollingSalesOrders":
                            trigger.Priority = 1;
                            break;

                        case "PollingPurchaseOrders":
                            trigger.Priority = 2;
                            break;

                    }
                    await Scheduler.ScheduleJob(job, trigger, cancellationToken);
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Scheduler.Shutdown(cancellationToken);
        }

        private static ITrigger CreateTrigger(JobSchedule schedule)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobType.FullName}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
        }

        private static ITrigger CreateTrigger(JobSchedule schedule, string cronExpression)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobType.FullName}.trigger.{Guid.NewGuid()}")
                .WithCronSchedule(cronExpression)
                .WithDescription(cronExpression)
                .Build();
        }

        private static IJobDetail CreateJob(JobSchedule schedule)
        {
            var jobType = schedule.JobType;
            return JobBuilder
                .Create(jobType)
                .WithIdentity($"{jobType.FullName}.{Guid.NewGuid()}")
                .WithDescription(jobType.Name)
                .Build();
        }
    }
}
