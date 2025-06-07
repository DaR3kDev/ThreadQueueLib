using System.Collections.Concurrent;
using ThreadQueueLib.Core.Interfaces;
using ThreadQueueLib.Events;
using ThreadQueueLib.Tasks;
using ThreadQueueLib.Tasks.Interfaces;
using ThreadQueueLib.Utils;

namespace ThreadQueueLib.Core;

/// <summary>
/// Cola genérica de tareas con soporte para persistencia opcional y circuito de saturación.
/// </summary>
public class TaskQueue<T> : ITaskQueue<T>
{
    private readonly TaskQueueOptions _options;
    private readonly ITaskQueuePersistence<T>? _persistence;
    private readonly ConcurrentQueue<QueuedTaskItem<T>> _inMemoryQueue = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly List<Task> _workers = [];
    private readonly CancellationTokenSource _internalCts = new();
    private CancellationTokenSource? _linkedCts;
    private readonly CircuitBreaker _circuitBreaker;
    private bool _isRunning;

    public TaskQueue(TaskQueueOptions options, ITaskQueuePersistence<T>? persistence = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _persistence = persistence;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrency);
        _circuitBreaker = new CircuitBreaker(_options.CircuitBreakerMaxFailures, _options.CircuitBreakerOpenDuration);
    }

    public async Task EnqueueAsync(QueuedTaskItem<T> task, CancellationToken cancellationToken = default)
    {
        if (task is null)
            throw new ArgumentNullException(nameof(task));

        if (_persistence is not null)
        {
            await _persistence.EnqueueAsync(task).ConfigureAwait(false);
        }
        else
        {
            if (_inMemoryQueue.Count >= _options.MaxQueueSize)
                throw new InvalidOperationException("Queue is full");

            _inMemoryQueue.Enqueue(task);
        }

        TaskQueueEvents<T>.RaiseTaskEnqueued(task);
    }

    public void Start(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _isRunning = true;
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

        for (int i = 0; i < _options.MaxConcurrency; i++)
        {
            _workers.Add(Task.Run(() => WorkerLoopAsync(_linkedCts.Token), cancellationToken));
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _internalCts.Cancel();
        _linkedCts?.Cancel();

        try
        {
            Task.WaitAll([.. _workers]);
        }
        catch (AggregateException ae)
        {
            ae.Handle(ex => ex is OperationCanceledException or TaskCanceledException);
        }
        finally
        {
            _isRunning = false;
            _workers.Clear();
            _semaphore.Dispose();
            _internalCts.Dispose();
            _linkedCts?.Dispose();
        }
    }

    private async Task<QueuedTaskItem<T>?> DequeueAsync()
    {
        if (_persistence != null)
            return await _persistence.DequeueAsync().ConfigureAwait(false);

        _inMemoryQueue.TryDequeue(out var task);
        return task;
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_circuitBreaker.IsOpen)
            {
                await DelaySafeAsync(1000, cancellationToken);
                continue;
            }

            var taskItem = await DequeueAsync().ConfigureAwait(false);

            if (taskItem is null)
            {
                await DelaySafeAsync(100, cancellationToken);
                continue;
            }

            await ProcessTaskAsync(taskItem, cancellationToken);
        }
    }

    private async Task DelaySafeAsync(int milliseconds, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(milliseconds, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProcessTaskAsync(QueuedTaskItem<T> taskItem, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            TaskQueueEvents<T>.RaiseTaskStarted(taskItem);

            await RetryPolicy.ExecuteAsync(
                () => taskItem.Task.ExecuteAsync(taskItem.Payload, cancellationToken),
                _options.RetryCount,
                _options.RetryDelay,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            _circuitBreaker.OnSuccess();
            TaskQueueEvents<T>.RaiseTaskCompleted(taskItem);
        }
        catch (OperationCanceledException)
        {
            TaskQueueEvents<T>.RaiseTaskCancelled(taskItem);
        }
        catch (Exception ex)
        {
            _circuitBreaker.OnFailure();
            TaskQueueEvents<T>.RaiseTaskFailed(taskItem, ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
