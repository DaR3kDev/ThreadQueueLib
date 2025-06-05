using ThreadQueueLib.Core;
using ThreadQueueLib.Extensions;
using ThreadQueueLib.Tasks.Interfaces;
using ThreadQueueTestApp.Models;


namespace ThreadQueueTestApp.Tests;

public class SaturationStressTest
{
    [Fact]
    public async Task Queue_Should_Handle_HighLoad_And_Reject_Overflow()
    {
        var options = new TaskQueueOptions
        {
            MaxConcurrency = 5,
            MaxQueueSize = 50,
            RetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            CircuitBreakerMaxFailures = 100,
            CircuitBreakerOpenDuration = TimeSpan.FromSeconds(10)
        };

        var queue = new TaskQueue<EmailPayload>(options);
        queue.Start();

        int acceptedTasks = 0;
        int rejectedTasks = 0;

        const int totalTasksToEnqueue = 1000;

        IQueuedTask<EmailPayload>[] tasks = new IQueuedTask<EmailPayload>[options.MaxQueueSize];

        for (int i = 0; i < totalTasksToEnqueue; i++)
        {
            var payload = new EmailPayload
            {
                To = $"user{i}@stress.com",
                Subject = "Test saturación extrema",
                Body = "Mensaje para prueba de carga muy alta"
            };

            var task = new SendEmailTaskLongRunning();

            try
            {
                await queue.EnqueueTask(task, payload);
                if (acceptedTasks < options.MaxQueueSize)
                    tasks[acceptedTasks] = task;

                acceptedTasks++;
            }
            catch (InvalidOperationException)
            {
                rejectedTasks++;
            }
            catch (Exception)
            {
                rejectedTasks++;
            }
        }

        var acceptedCompletionTasks = Array.FindAll(tasks, t => t != null);

        if (acceptedCompletionTasks.Length > 0)
        {
            await Task.WhenAll(acceptedCompletionTasks
                              .Select(t => ((SendEmailTaskLongRunning)t).CompletionSource.Task))
                              .WaitAsync(TimeSpan.FromSeconds(30));
        }

        queue.Stop();

        Console.WriteLine($"Aceptadas: {acceptedTasks}, Rechazadas: {rejectedTasks}");

        Assert.True(rejectedTasks > 0, "La cola debería rechazar tareas cuando está llena");
        Assert.InRange(acceptedTasks, 1, totalTasksToEnqueue);
    }
}

