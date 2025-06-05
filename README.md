# ThreadQueueLib

ThreadQueueLib es una librería en C# para la gestión y concurrente de colas de tareas genéricas con soporte para:

* Control de concurrencia (número máximo de tareas paralelas)
* Prioridades en las tareas
* Reintentos configurables con backoff exponencial y jitter
* Eventos para monitoreo (inicio, éxito, error, cancelación)
* Arquitectura limpia
* Opcional: persistencia externa para escalabilidad (no incluida en esta versión base)

---

## Estructura del proyecto

```
ThreadQueueLib/
│
├── Core/
│   ├── ITaskQueue.cs           # Interfaz principal de la cola
│   ├── TaskQueue.cs            # Implementación concurrente de la cola
│   └── TaskQueueOptions.cs     # Configuración de la cola (concurrencia, retries, etc)
│
├── Tasks/
│   ├── IQueuedTask.cs          # Contrato para tareas que se pueden encolar
│   ├── QueuedTask.cs           # Implementación base de tarea
│   └── QueuedTaskItem.cs       # Contenedor de tarea con prioridad y metadata
│
├── Events/
│   └── TaskQueueEvents.cs      # Eventos para notificaciones (inicio, error, completado)
│
├── Utils/
│   └── RetryPolicy.cs          # Lógica de reintentos con backoff exponencial y jitter
│
└── Extensions/
    └── TaskQueueExtensions.cs # Métodos de extensión útiles
```

---

## Instalación

Agrega el proyecto a tu solución o compílalo como una librería DLL para usarlo en tus aplicaciones .NET 6/7/8/9.

---

## Uso básico

### Definir una tarea

```csharp
public class EmailTask : IQueuedTask<string>
{
    public async Task ExecuteAsync(string email, CancellationToken cancellationToken)
    {
        // Simula enviar email
        Console.WriteLine($"Enviando email a {email}...");
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Email enviado a {email}");
    }
}
```

### Configurar y crear la cola

```csharp
var options = new TaskQueueOptions
{
    MaxConcurrency = 3,
    MaxQueueSize = 100,
    RetryCount = 3,
    RetryDelay = TimeSpan.FromSeconds(2)
};

var queue = new TaskQueue<string>(options);
```

### Encolar tareas

```csharp
await queue.EnqueueAsync(new QueuedTaskItem<string>
{
    Task = new EmailTask(),
    Payload = "user@example.com",
    Priority = 1
});
```

### Iniciar y detener la cola

```csharp
queue.Start();

// Esperar algo o hacer otras cosas
Console.ReadLine();

queue.Stop();
```

---

## Eventos

Puedes subscribirte a eventos para monitorear el estado de las tareas:

```csharp
TaskQueueEvents<string>.OnTaskStarted += task => 
    Console.WriteLine($"Tarea {task.Id} iniciada.");

TaskQueueEvents<string>.OnTaskCompleted += task => 
    Console.WriteLine($"Tarea {task.Id} completada.");

TaskQueueEvents<string>.OnTaskFailed += (task, ex) =>
    Console.WriteLine($"Tarea {task.Id} falló: {ex.Message}");
```

---

## Características avanzadas

* **Retries con backoff exponencial y jitter** para evitar saturación en fallos.
* Uso de **SemaphoreSlim** para control de concurrencia.
* Arquitectura modular para extensión (puedes agregar persistencia, circuit breakers, etc).
* Manejo de cancelaciones con `CancellationToken`.

---

## Recomendaciones para producción

* Considera agregar persistencia externa (Redis, bases de datos) para asegurar durabilidad y recuperación de tareas.
* Integra circuit breakers para evitar saturar recursos ante fallos persistentes.
* Usa logging y monitoreo para rastrear tareas y detectar cuellos de botella.
* Valida y controla el tamaño máximo de la cola para evitar consumo excesivo de memoria.
* Considera seguridad si ejecutas código externo o en ambientes multiusuario.
* Usa cancelaciones para poder detener tareas correctamente al apagar o reiniciar.

---

## Licencia

MIT License. Puedes usar y modificar libremente esta librería.
