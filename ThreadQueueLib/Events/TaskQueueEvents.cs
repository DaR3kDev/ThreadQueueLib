using ThreadQueueLib.Tasks;

namespace ThreadQueueLib.Events;

/// <summary>
/// Clase estática que gestiona los eventos para el ciclo de vida de las tareas en la cola.
/// Utiliza genéricos para poder trabajar con cualquier tipo de tarea.
/// </summary>
/// <typeparam name="T">Tipo de dato que maneja la tarea encolada.</typeparam>
public static class TaskQueueEvents<T>
{
    /// <summary>
    /// Evento que se dispara cuando una tarea es encolada.
    /// </summary>
    public static event Action<QueuedTaskItem<T>>? OnTaskEnqueued;

    /// <summary>
    /// Evento que se dispara cuando una tarea comienza su ejecución.
    /// </summary>
    public static event Action<QueuedTaskItem<T>>? OnTaskStarted;

    /// <summary>
    /// Evento que se dispara cuando una tarea se completa exitosamente.
    /// </summary>
    public static event Action<QueuedTaskItem<T>>? OnTaskCompleted;

    /// <summary>
    /// Evento que se dispara cuando una tarea falla con una excepción.
    /// </summary>
    public static event Action<QueuedTaskItem<T>, Exception>? OnTaskFailed;

    /// <summary>
    /// Evento que se dispara cuando una tarea es cancelada.
    /// </summary>
    public static event Action<QueuedTaskItem<T>>? OnTaskCancelled;

    /// <summary>
    /// Método interno para invocar el evento OnTaskEnqueued de forma segura.
    /// </summary>
    /// <param name="task">La tarea que fue encolada.</param>
    internal static void RaiseTaskEnqueued(QueuedTaskItem<T> task) => OnTaskEnqueued?.Invoke(task);

    /// <summary>
    /// Método interno para invocar el evento OnTaskStarted de forma segura.
    /// </summary>
    /// <param name="task">La tarea que inició ejecución.</param>
    internal static void RaiseTaskStarted(QueuedTaskItem<T> task) => OnTaskStarted?.Invoke(task);

    /// <summary>
    /// Método interno para invocar el evento OnTaskCompleted de forma segura.
    /// </summary>
    /// <param name="task">La tarea que finalizó correctamente.</param>
    internal static void RaiseTaskCompleted(QueuedTaskItem<T> task) => OnTaskCompleted?.Invoke(task);

    /// <summary>
    /// Método interno para invocar el evento OnTaskFailed de forma segura, pasando la excepción que ocurrió.
    /// </summary>
    /// <param name="task">La tarea que falló.</param>
    /// <param name="ex">La excepción que causó el fallo.</param>
    internal static void RaiseTaskFailed(QueuedTaskItem<T> task, Exception ex) => OnTaskFailed?.Invoke(task, ex);

    /// <summary>
    /// Método interno para invocar el evento OnTaskCancelled de forma segura.
    /// </summary>
    /// <param name="task">La tarea que fue cancelada.</param>
    internal static void RaiseTaskCancelled(QueuedTaskItem<T> task) => OnTaskCancelled?.Invoke(task);
}
