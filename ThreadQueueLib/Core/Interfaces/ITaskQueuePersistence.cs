using ThreadQueueLib.Tasks;

namespace ThreadQueueLib.Core.Interfaces;

/// <summary>
/// Interfaz para persistencia de la cola (ej. Redis, DB, etc).
/// Permite desacoplar la implementación de almacenamiento de tareas.
/// </summary>
public interface ITaskQueuePersistence<T>
{
    Task EnqueueAsync(QueuedTaskItem<T> task);
    Task<QueuedTaskItem<T>?> DequeueAsync();
    Task<int> GetQueueLengthAsync();
}

