namespace ThreadQueueLib.Utils;

/// <summary>
/// Proporciona una política simple de reintentos para ejecutar acciones asíncronas con posibilidad de reintentar en caso de fallo.
/// </summary>
public class RetryPolicy
{
    // Defino Random estático para usar en el jitter (aleatoriedad)
    private static readonly Random _jitterer = new();

    /// <summary>
    /// Ejecuta una acción asíncrona con reintentos automáticos en caso de excepción.
    /// </summary>
    /// <param name="action">La función asíncrona que se intentará ejecutar.</param>
    /// <param name="maxRetries">Número máximo de reintentos permitidos después del primer intento.</param>
    /// <param name="delay">Tiempo base de espera entre cada intento de reintento.</param>
    /// <param name="cancellationToken">Token para cancelar la operación durante el delay.</param>
    /// <returns>Una tarea que completa cuando la acción se ejecuta con éxito o se agotan los reintentos.</returns>
    public static async Task ExecuteAsync(
        Func<Task> action,
        int maxRetries,
        TimeSpan delay,  // tiempo base de espera (antes era "baseDelay" pero lo llamamos "delay" acá)
        CancellationToken cancellationToken)
    {
        int retries = 0;

        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch when (retries < maxRetries)
            {
                retries++;

                // Calculamos jitter aleatorio (0 a 100 ms)
                var jitter = TimeSpan.FromMilliseconds(_jitterer.Next(0, 100));

                // Calculamos delay con backoff exponencial + jitter
                var retryDelay = TimeSpan.FromMilliseconds(Math.Pow(2, retries) * delay.TotalMilliseconds) + jitter;

                await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }
}
