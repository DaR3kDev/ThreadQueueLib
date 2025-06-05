namespace ThreadQueueLib.Tasks.Interfaces;

/// <summary>
/// Define las operaciones básicas que debe soportar una cola de tareas genérica.
/// Permite encolar, iniciar, detener y consultar el estado de la cola.
/// </summary>
/// <typeparam name="TPayload">Tipo de datos que representan la carga útil de cada tarea.</typeparam>
public interface ITaskQueue<T>
{
    /// <summary>
    /// Encola una tarea para ser procesada.
    /// </summary>
    /// <param name="task">La tarea que se ejecutará.</param>
    /// <param name="payload">Datos asociados a la tarea.</param>
    /// <param name="priority">Prioridad de la tarea (mayor valor = mayor prioridad).</param>
    /// <returns>Tarea asincrónica que se completa cuando la tarea ha sido aceptada en la cola.</returns>
    Task EnqueueAsync(QueuedTaskItem<T> task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia el procesamiento de la cola.
    /// </summary>
    void Start(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detiene el procesamiento de la cola.
    /// </summary>
    void Stop();
}

