using ThreadQueueLib.Tasks.Interfaces;

namespace ThreadQueueTestApp.Models;

public class FailingEmailTask : IQueuedTask<EmailPayload>
{
    public TaskCompletionSource<bool> CompletionSource { get; } = new();

    public async Task ExecuteAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(50, cancellationToken);
            throw new Exception("Fail simulated");
        }
        catch (Exception ex)
        {
            CompletionSource.TrySetException(ex);
            throw;
        }
    }
}

