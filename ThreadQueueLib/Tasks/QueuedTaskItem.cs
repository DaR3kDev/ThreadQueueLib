using ThreadQueueLib.Tasks.Interfaces;

namespace ThreadQueueLib.Tasks;

/// <summary>
/// Representa un ítem (unidad) de trabajo que será encolado y procesado.
/// Contiene la tarea, su carga útil, prioridad, estado de reintentos y metadatos.
/// </summary>
/// <typeparam name="T">Tipo de la carga útil asociada a la tarea.</typeparam>
public sealed class QueuedTaskItem<T>
{
    /// <summary>
    /// Identificador único para esta tarea encolada.
    /// Se genera automáticamente al crear la instancia.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// La tarea que se ejecutará.
    /// Debe implementar la interfaz <see cref="IQueuedTask{T}"/>.
    /// </summary>
    public required IQueuedTask<T> Task { get; init; }

    /// <summary>
    /// Datos o información que la tarea necesita para su ejecución.
    /// </summary>
    public required T Payload { get; init; }

    /// <summary>
    /// Prioridad de la tarea dentro de la cola.
    /// Las tareas con mayor prioridad se procesan primero.
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Contador de intentos realizados para ejecutar esta tarea.
    /// Se incrementa cada vez que se ejecuta la tarea y falla.
    /// </summary>
    public int RetryAttempts { get; set; } = 0;

    /// <summary>
    /// Fecha y hora UTC en la que la tarea fue encolada.
    /// Útil para monitoreo y métricas.
    /// </summary>
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
}
