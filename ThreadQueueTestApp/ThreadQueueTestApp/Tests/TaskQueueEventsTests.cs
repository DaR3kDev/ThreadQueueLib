using ThreadQueueLib.Core;
using ThreadQueueLib.Events;
using ThreadQueueLib.Extensions;
using ThreadQueueLib.Tasks.Interfaces;
using ThreadQueueTestApp.Models;

namespace ThreadQueueTestApp.Tests;

public class TaskQueueEventsTests
{
    [Fact]
    public async Task TaskQueue_Should_Raise_Events_Correctly()
    {

        var options = new TaskQueueOptions
        {
            MaxConcurrency = 1,
            MaxQueueSize = 10,
            RetryCount = 0,
            RetryDelay = TimeSpan.Zero,
            CircuitBreakerMaxFailures = 3,
            CircuitBreakerOpenDuration = TimeSpan.FromSeconds(5)
        };

        var queue = new TaskQueue<EmailPayload>(options);

        int startedCount = 0;
        int completedCount = 0;
        int failedCount = 0;

        // Suscribir a eventos
        TaskQueueEvents<EmailPayload>.OnTaskStarted += _ => Interlocked.Increment(ref startedCount);
        TaskQueueEvents<EmailPayload>.OnTaskCompleted += _ => Interlocked.Increment(ref completedCount);
        TaskQueueEvents<EmailPayload>.OnTaskFailed += (_, __) => Interlocked.Increment(ref failedCount);

        queue.Start();

        // Tarea que siempre falla para probar OnTaskFailed
        var failingTask = new FailingEmailTask();
        var payloadFail = new EmailPayload { To = "fail@test.com", Subject = "Fail", Body = "Fail body" };

        // Tarea que siempre pasa
        var successTask = new SendEmailTask();
        var payloadSuccess = new EmailPayload { To = "success@test.com", Subject = "Success", Body = "Success body" };

        // Encolar ambas tareas
        await queue.EnqueueTask(failingTask, payloadFail);
        await queue.EnqueueTask(successTask, payloadSuccess);

        // Esperar tareas completadas o fallidas
        await Task.WhenAll(
            failingTask.CompletionSource.Task.ContinueWith(_ => { }),
            successTask.CompletionSource.Task.ContinueWith(_ => { })
    )   .WaitAsync(TimeSpan.FromSeconds(10));
        TaskQueueEvents<EmailPayload>.OnTaskFailed += (task, ex) =>
    Console.WriteLine($"Tarea falló: {ex.Message}");
        queue.Stop();

        // Validaciones
        Assert.Equal(2, startedCount);  // Se iniciaron las dos tareas
        Assert.Equal(1, completedCount); // Solo una completada con éxito
        Assert.Equal(1, failedCount);    // Una falló
    }
}
