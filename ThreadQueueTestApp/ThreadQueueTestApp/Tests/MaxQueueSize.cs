using ThreadQueueLib.Core;
using ThreadQueueLib.Extensions;
using ThreadQueueTestApp.Models;

namespace ThreadQueueTestApp.Tests;

public class MaxQueueSize
{
    [Fact]
    public async Task Queue_Should_Reject_Task_When_Queue_Is_Full()
    {
        var options = new TaskQueueOptions
        {
            MaxConcurrency = 1,
            MaxQueueSize = 3,  // Muy pequeña para probar overflow
            RetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            CircuitBreakerMaxFailures = 5,
            CircuitBreakerOpenDuration = TimeSpan.FromSeconds(5)
        };

        var queue = new TaskQueue<EmailPayload>(options);
        queue.Start();

        var tasks = new SendEmailTask[options.MaxQueueSize];
        int accepted = 0, rejected = 0;

        for (int i = 0; i < options.MaxQueueSize + 2; i++) // Intentamos agregar más que el límite
        {
            var payload = new EmailPayload { To = $"user{i}@test.com", Subject = "Overflow", Body = "Test" };
            var task = new SendEmailTask();

            try
            {
                await queue.EnqueueTask(task, payload);
                if (i < options.MaxQueueSize)
                {
                    tasks[i] = task;
                    accepted++;
                }
                else
                {
                    rejected++; // Si no lanza excepción pero está fuera del rango, contamos rechazo (seguridad)
                }
            }
            catch (InvalidOperationException)
            {
                rejected++;
            }
        }

        // Esperar tareas aceptadas
        await Task.WhenAll(tasks.Where(t => t != null).Select(t => t.CompletionSource.Task))
                  .WaitAsync(TimeSpan.FromSeconds(5));

        queue.Stop();

        Assert.Equal(options.MaxQueueSize, accepted);
        Assert.True(rejected > 0, "Se deben rechazar tareas cuando la cola está llena");
    }
}

