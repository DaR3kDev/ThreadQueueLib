using ThreadQueueLib.Tasks.Interfaces;

namespace ThreadQueueLib.Tasks;

public abstract class QueuedTask<T> : IQueuedTask<T>
{
    public abstract Task ExecuteAsync(T payload, CancellationToken cancellationToken);
}

