using ThreadQueueLib.Core;
using ThreadQueueLib.Events;
using ThreadQueueLib.Extensions;
using ThreadQueueLib.Tasks.Interfaces;

public class EmailPayload
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}

public class SendEmailTask : IQueuedTask<EmailPayload>
{
    public async Task ExecuteAsync(EmailPayload payload, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Enviando email a {payload.To}...");
        await Task.Delay(1000, cancellationToken); 
        Console.WriteLine($"Email enviado a {payload.To}");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        // Subscribirse a eventos
        TaskQueueEvents<EmailPayload>.OnTaskStarted += task =>
            Console.WriteLine($"[INICIO] Tarea {task.Id} con prioridad {task.Priority}");

        TaskQueueEvents<EmailPayload>.OnTaskCompleted += task =>
            Console.WriteLine($"[FIN] Tarea {task.Id}");

        TaskQueueEvents<EmailPayload>.OnTaskFailed += (task, ex) =>
            Console.WriteLine($"[ERROR] Tarea {task.Id}: {ex.Message}");

        var options = new TaskQueueOptions
        {
            MaxConcurrency = 2,
            MaxQueueSize = 10,
            RetryCount = 2,
            RetryDelay = TimeSpan.FromSeconds(1)
        };

        var queue = new TaskQueue<EmailPayload>(options);

        queue.Start();

        // Encolar tareas
        for (int i = 0; i < 5; i++)
        {
            var payload = new EmailPayload
            {
                To = $"user{i}@test.com",
                Subject = "Hola",
                Body = "Esto es un mensaje de prueba"
            };

            await queue.EnqueueTask(new SendEmailTask(), payload, priority: i);
        }

        Console.WriteLine("Tareas encoladas. Esperando procesamiento...");

        await Task.Delay(8000);

        queue.Stop();

        Console.WriteLine("Cola detenida. Presiona cualquier tecla para salir.");
        Console.ReadKey();
    }
}
