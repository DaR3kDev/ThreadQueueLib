using System.Collections.Concurrent;
using ThreadQueueLib.Events;
using ThreadQueueLib.Tasks;
using ThreadQueueLib.Tasks.Interfaces;
using ThreadQueueLib.Utils;

namespace ThreadQueueLib.Core;

/// <summary>
/// Cola genérica para gestionar y ejecutar tareas con concurrencia controlada y reintentos.
/// Incluye validación, manejo seguro de excepciones, cancelación y eventos para monitoreo.
/// </summary>
/// <typeparam name="T">Tipo de datos que maneja la tarea.</typeparam>
public class TaskQueue<T> : ITaskQueue<T>
{
    private readonly TaskQueueOptions _options;
    private readonly ConcurrentQueue<QueuedTaskItem<T>> _queue = new();
    private readonly SemaphoreSlim _semaphore;
    private readonly List<Task> _workers = new();
    private readonly CancellationTokenSource _internalCts = new();
    private bool _isRunning;

    /// <summary>
    /// Constructor que recibe las opciones para configurar la cola.
    /// Se validan las opciones para evitar valores inválidos.
    /// </summary>
    public TaskQueue(TaskQueueOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.MaxConcurrency <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxConcurrency), "MaxConcurrency debe ser > 0");
        if (_options.MaxQueueSize <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxQueueSize), "MaxQueueSize debe ser > 0");
        if (_options.RetryCount < 0) throw new ArgumentOutOfRangeException(nameof(options.RetryCount), "RetryCount no puede ser negativo");
        if (_options.RetryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.RetryDelay), "RetryDelay no puede ser negativo");

        _semaphore = new SemaphoreSlim(_options.MaxConcurrency);
    }

    /// <summary>
    /// Encola una tarea para ser procesada.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si la cola está llena.</exception>
    /// <exception cref="ArgumentNullException">Si la tarea es nula.</exception>
    public Task EnqueueAsync(QueuedTaskItem<T> task, CancellationToken cancellationToken = default)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (_queue.Count >= _options.MaxQueueSize)
            throw new InvalidOperationException("Queue is full");

        _queue.Enqueue(task);
        TaskQueueEvents<T>.RaiseTaskEnqueued(task);  // Evento para monitoreo

        return Task.CompletedTask;
    }

    /// <summary>
    /// Inicia los trabajadores para procesar las tareas encoladas.
    /// </summary>
    public void Start(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;
        _isRunning = true;

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

        for (int i = 0; i < _options.MaxConcurrency; i++)
        {
            _workers.Add(Task.Run(() => WorkerLoop(linkedCts.Token), linkedCts.Token));
        }
    }

    /// <summary>
    /// Detiene el procesamiento y cancela las tareas en ejecución.
    /// Espera hasta 10 segundos para que los workers terminen ordenadamente.
    /// </summary>
    public void Stop()
    {
        if (!_isRunning) return;

        _internalCts.Cancel();

        try
        {
            Task.WaitAll(_workers.ToArray(), TimeSpan.FromSeconds(10));
        }
        catch (AggregateException ex)
        {
            // Manejar excepciones por cancelación u otras
            foreach (var inner in ex.InnerExceptions)
            {
                Console.Error.WriteLine($"Error deteniendo worker: {inner.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error inesperado deteniendo cola: {ex.Message}");
        }

        _isRunning = false;
    }

    /// <summary>
    /// Loop que cada trabajador ejecuta para procesar tareas.
    /// Maneja errores, cancelación y reporta eventos.
    /// </summary>
    private async Task WorkerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_queue.TryDequeue(out var taskItem))
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

                    TaskQueueEvents<T>.RaiseTaskCompleted(taskItem);
                }
                catch (OperationCanceledException)
                {
                    // Task cancelada correctamente, no es error
                    TaskQueueEvents<T>.RaiseTaskCancelled(taskItem);
                }
                catch (Exception ex)
                {
                    TaskQueueEvents<T>.RaiseTaskFailed(taskItem, ex);
                    Console.Error.WriteLine($"Error ejecutando tarea {taskItem.Id}: {ex}");
                }
                finally
                {
                    _semaphore.Release();
                }
            }, cancellationToken);
        }
    }
}
