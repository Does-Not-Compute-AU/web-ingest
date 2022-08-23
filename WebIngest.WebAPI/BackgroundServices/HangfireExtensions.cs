using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace WebIngest.WebAPI.BackgroundServices
{
    public static class HangfireExtensions
    {
        private static readonly BackgroundJobClient JobClient = new()
        {
            RetryAttempts = 0
        };

        private static readonly ConcurrentQueue<Tuple<string, Expression<Action>>> JobsToQueue = new();
        private static readonly object _daemonLock = new();
        private static bool _daemonRunning;

        private static void WakeQueueDaemon()
        {
            lock (_daemonLock)
            {
                if (!_daemonRunning)
                {
                    Task.Run(() =>
                    {
                        _daemonRunning = true;
                        while (JobsToQueue.Any())
                        {
                            Tuple<string, Expression<Action>> jobPair;
                            while (!JobsToQueue.TryDequeue(out jobPair))
                                Thread.Sleep(10);

                            var state = new EnqueuedState(jobPair.Item1);
                            JobClient.Create(jobPair.Item2, state);

                            Thread.Sleep(10);
                        }
                        _daemonRunning = false;
                    });
                }
            }
        }

        public static void EnqueueBackgroundJob(string queueName, Expression<Action> job)
        {
            JobsToQueue.Enqueue(new(queueName, job));
            WakeQueueDaemon();
        }

        public static void PurgeAllQueues(this IMonitoringApi monitor)
        {
            foreach (QueueWithTopEnqueuedJobsDto queue in monitor.Queues())
            {
                monitor.PurgeQueue(queue.Name);
            }
        }

        public static void PurgeQueue(this IMonitoringApi monitor, string queueName)
        {
            var toDelete = new List<string>();
            var queue = monitor.Queues().FirstOrDefault(x => x.Name == queueName);
            if (queue != null)
                for (var i = 0; i < Math.Ceiling(queue.Length / 1000d); i++)
                {
                    monitor
                        .EnqueuedJobs(queue.Name, 1000 * i, 1000)
                        .ForEach(x =>
                            toDelete.Add(x.Key)
                        );
                }

            foreach (var jobId in toDelete)
            {
                BackgroundJob.Delete(jobId);
            }
        }
    }
}