namespace ThreadQueueLib.Tasks.Interfaces;
public interface IQueuedTask<T>
{
    Task ExecuteAsync(T payload, CancellationToken cancellationToken);
}

