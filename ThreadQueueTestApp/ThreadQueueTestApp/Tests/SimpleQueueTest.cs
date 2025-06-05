using ThreadQueueLib.Core;
using ThreadQueueLib.Extensions;
using ThreadQueueTestApp.Models;

namespace ThreadQueueTestApp.Tests;

public class SimpleQueueTest
{
    [Fact]
    public async Task Queue_Should_Process_All_Tasks_Correctly()
    {
        var options = new TaskQueueOptions
        {
            MaxConcurrency = 3,
            MaxQueueSize = 10,
            RetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            CircuitBreakerMaxFailures = 5,
            CircuitBreakerOpenDuration = TimeSpan.FromSeconds(5)
        };

        var queue = new TaskQueue<EmailPayload>(options);
        queue.Start();

        const int totalTasks = 5;
        var tasks = new SendEmailTask[totalTasks];

        for (int i = 0; i < totalTasks; i++)
        {
            var payload = new EmailPayload
            {
                To = $"user{i}@test.com",
                Subject = "Test simple queue",
                Body = "Mensaje simple para prueba"
            };

            var task = new SendEmailTask();
            tasks[i] = task;

            await queue.EnqueueTask(task, payload);
        }

        // Esperar que todas las tareas terminen
        await Task.WhenAll(tasks.Select(t => t.CompletionSource.Task))
                  .WaitAsync(TimeSpan.FromSeconds(10));

        queue.Stop();

        // Asegurar que todas las tareas fueron completadas
        Assert.All(tasks, t => Assert.True(t.CompletionSource.Task.IsCompletedSuccessfully));
    }
}
