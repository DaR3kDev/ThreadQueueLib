using ThreadQueueLib.Tasks.Interfaces;


namespace ThreadQueueTestApp.Models;

public class EmailPayload
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}

public class SendEmailTask : IQueuedTask<EmailPayload>
{
    public TaskCompletionSource<bool> CompletionSource { get; } = new();

    public async Task ExecuteAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simula envío rápido
        CompletionSource.TrySetResult(true);
    }
}

public class SendEmailTaskLongRunning : IQueuedTask<EmailPayload>
{
    public TaskCompletionSource<bool> CompletionSource { get; } = new();

    public async Task ExecuteAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken); // Simula tarea pesada
        CompletionSource.TrySetResult(true);
    }
}

