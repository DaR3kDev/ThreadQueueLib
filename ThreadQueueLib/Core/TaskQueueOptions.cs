namespace ThreadQueueLib.Core;

/// <summary>
/// Opciones de configuración para la cola de tareas.
/// Permite ajustar el comportamiento de concurrencia, tamaño de cola y reintentos.
/// </summary>
public class TaskQueueOptions
{
    /// <summary>
    /// Máximo número de tareas que pueden ejecutarse en paralelo.
    /// Controla la cantidad de hilos concurrentes permitidos.
    /// </summary>
    public required int MaxConcurrency { get; init; }

    /// <summary>
    /// Tamaño máximo permitido para la cola de tareas en espera.
    /// Define cuántas tareas pueden estar pendientes antes de rechazar nuevas.
    /// </summary>
    public required int MaxQueueSize { get; init; }

    /// <summary>
    /// Número máximo de intentos que se realizarán para reintentar una tarea que falle.
    /// </summary>
    public required int RetryCount { get; init; }

    /// <summary>
    /// Tiempo de espera entre cada intento de reintento cuando una tarea falla.
    /// Permite dar un respiro antes de volver a ejecutar la tarea.
    /// </summary>
    public required TimeSpan RetryDelay { get; init; }
}
