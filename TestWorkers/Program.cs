using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestWorkers
{
    class Job
    {
        public int Id { get; set; }
        public TimeSpan Duration { get; set; }
    }

    class Worker
    {
        public int Id { get; set; }
        public async Task DoTaskAsync(Job job)
        {
            Console.WriteLine($"Worker {Id} started the job {job.Id} that will take {job.Duration.TotalSeconds}s");
            await Task.Delay(job.Duration);
            Console.WriteLine($"Worker {Id} finished the job {job.Id} after {job.Duration.TotalSeconds}s");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var jobs = GenerateJobs();
            var tasks = new Task[jobs.Count];
            Parallel.For(0, jobs.Count, (index) =>
            {
                var worker = new Worker { Id = index + 1 };
                tasks[index] = worker.DoTaskAsync(jobs[index]);
            });

            Task.WaitAll(tasks);
            Console.WriteLine("All the work is done!");
            Console.ReadLine();
        }

        static List<Job> GenerateJobs()
        {
            var random = new Random();
            var nbOfJobs = random.Next(2, 5);
            var jobs = new List<Job>(nbOfJobs);
            for (var i = 0; i < nbOfJobs; i++)
            {
                jobs.Add(new Job { Id = i + 1, Duration = TimeSpan.FromSeconds(random.Next(1, 7)) });
            }
            return jobs;
        }
    }
}
