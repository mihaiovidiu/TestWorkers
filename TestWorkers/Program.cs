using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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

    class WorkersTeam
    {
        private readonly Worker[] _workers;
        public WorkersTeam(int nbOfWorkers)
        {
            _workers = new Worker[nbOfWorkers];
            for (var i = 0; i < nbOfWorkers; i++)
            {
                _workers[i] = new Worker { Id = i + 1 };
            }
        }

        public void DoJobs(IEnumerable<Job> jobs)
        {
            var workersQueue = new ConcurrentQueue<Worker>(_workers);
            var workersAvailableEvent = new AutoResetEvent(false);
            var runningTasks = new List<Task>();

            Parallel.ForEach(jobs, async job =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"-- Trying to get a worker on thread {threadId}");

                Worker worker = null;

                do 
                {
                    if (!workersQueue.TryDequeue(out worker))
                    {
                        // No free workers
                        Console.WriteLine($"-- Blocking until a worker is free on thread {threadId}");
                        // Wait until signal that free worker is in the queue
                        workersAvailableEvent.WaitOne();
                        // Current thread is blocked here
                        Console.WriteLine($"-- Worker available event received on thread {threadId}");
                    }
                }
                while (worker == null);

                // Got an worker
                Console.WriteLine($"-- Got worker {worker.Id} on thread {threadId}");

                var task = worker.DoTaskAsync(job);
                runningTasks.Add(task);
                await task;
                workersQueue.Enqueue(worker);
                // Free worker added to queue
                // Signal the event
                workersAvailableEvent.Set();
            });

            // Wait for all the task to complete
            Task.WaitAll(runningTasks.ToArray());

        }
    }

    class Program
    {
        static void Main(string[] args)
        {

            var (numberOfJobs, numberOfWorkers) = GetProgramArguments(args);
            
            var jobs = GenerateJobs(numberOfJobs);
            var team = new WorkersTeam(numberOfWorkers);

            team.DoJobs(jobs);

            Console.WriteLine("All the work is done!");
            Console.WriteLine("Press any key to end the program");
            Console.ReadKey(true);
        }

        static List<Job> GenerateJobs(int nbOfJobs)
        {
            var random = new Random();
            var jobs = new List<Job>(nbOfJobs);
            for (var i = 0; i < nbOfJobs; i++)
            {
                jobs.Add(new Job { Id = i + 1, Duration = TimeSpan.FromSeconds(random.Next(1, 7)) });
            }
            return jobs;
        }

        static (int, int) GetProgramArguments(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("Please provide the number of jobs and the number of workers as arguments");
            }

            if (!int.TryParse(args[0], out int nbOfJobs))
            {
                throw new ArgumentException("First argument must be integer");
            }

            if (!int.TryParse(args[1], out int nbOfWorkers))
            {
                throw new ArgumentException("Second argument must be integer");
            }

            return (nbOfJobs, nbOfWorkers);
        }
    }
}
