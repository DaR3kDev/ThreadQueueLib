using System.Collections.Concurrent;
using ThreadQueueLib.Core.Interfaces;
using ThreadQueueLib.Events;
using ThreadQueueLib.Tasks;
using ThreadQueueLib.Tasks.Interfaces;
using ThreadQueueLib.Utils;

namespace ThreadQueueLib.Core;

/// <summary>
/// Cola de tareas genérica con soporte opcional para persistencia y circuito de saturación.
/// </summary>
public class TaskQueue<T>(TaskQueueOptions options, ITaskQueuePersistence<T>? persistence = null) : ITaskQueue<T>
{
    private readonly TaskQueueOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ITaskQueuePersistence<T>? _persistence = persistence;
    private readonly ConcurrentQueue<QueuedTaskItem<T>> _inMemoryQueue = new();
    private readonly SemaphoreSlim _semaphore = new(options.MaxConcurrency);
    private readonly List<Task> _workers = [];
    private readonly CancellationTokenSource _internalCts = new();
    private readonly CircuitBreaker _circuitBreaker = new(options.CircuitBreakerMaxFailures, options.CircuitBreakerOpenDuration);
    private bool _isRunning;

    /// <summary>
    /// Encola una tarea, con persistencia si está configurada.
    /// </summary>
    public async Task EnqueueAsync(QueuedTaskItem<T> task, CancellationToken cancellationToken = default)
    {
        if (_persistence is not null)
        {
            await _persistence.EnqueueAsync(task);
            TaskQueueEvents<T>.RaiseTaskEnqueued(task);
            return;
        }

        if (_inMemoryQueue.Count >= _options.MaxQueueSize)
            throw new InvalidOperationException("Queue is full");

        _inMemoryQueue.Enqueue(task);
        TaskQueueEvents<T>.RaiseTaskEnqueued(task);
    }

    /// <summary>
    /// Inicia los workers para procesar tareas en paralelo.
    /// </summary>
    public void Start(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;
        _isRunning = true;

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

        for (int i = 0; i < _options.MaxConcurrency; i++)
            _workers.Add(Task.Run(() => WorkerLoop(linkedCts.Token), linkedCts.Token));
    }

    /// <summary>
    /// Detiene la cola y espera a que terminen los workers.
    /// </summary>
    public void Stop()
    {
        _internalCts.Cancel();
        try
        {
            Task.WaitAll(_workers);
        }
        catch (AggregateException ae)
        {
            ae.Handle(ex => ex is TaskCanceledException);
        }
        _isRunning = false;
    }

    /// <summary>
    /// Obtiene la siguiente tarea desde persistencia o memoria.
    /// </summary>
    private async Task<QueuedTaskItem<T>?> DequeueAsync()
    {
        if (_persistence != null)
            return await _persistence.DequeueAsync();

        _inMemoryQueue.TryDequeue(out var task);
        return task;
    }

    /// <summary>
    /// Bucle principal que ejecuta tareas.
    /// </summary>
    private async Task WorkerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_circuitBreaker.IsOpen)
            {
                await Task.Delay(1000, cancellationToken);
                continue;
            }

            var taskItem = await DequeueAsync();
            if (taskItem is null)
            {
                await Task.Delay(100, cancellationToken);
                continue;
            }

            await _semaphore.WaitAsync(cancellationToken);

            _ = Task.Run(async () =>
            {
                try
                {
                    TaskQueueEvents<T>.RaiseTaskStarted(taskItem);

                    await RetryPolicy.ExecuteAsync(
                        () => taskItem.Task.ExecuteAsync(taskItem.Payload, cancellationToken),
                        _options.RetryCount,
                        _options.RetryDelay,
                        cancellationToken
                    );

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
            }, cancellationToken);
        }
    }
}
