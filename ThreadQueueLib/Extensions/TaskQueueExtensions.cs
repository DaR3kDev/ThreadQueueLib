using ThreadQueueLib.Tasks;
using ThreadQueueLib.Tasks.Interfaces;

namespace ThreadQueueLib.Extensions;

public static class TaskQueueExtensions
{
    public static Task EnqueueTask<T>(this ITaskQueue<T> queue, IQueuedTask<T> task, T payload, int priority = 0)
    {
        return queue.EnqueueAsync(new QueuedTaskItem<T>
        {
            Task = task,
            Payload = payload,
            Priority = priority
        });
    }
}

