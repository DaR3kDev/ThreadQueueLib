namespace ThreadQueueLib.Utils;

/// <summary>
/// Proporciona una política simple y configurable de reintentos para ejecutar acciones asíncronas.
/// </summary>
public static class RetryPolicy
{
    private static readonly Random _jitterer = new();

    /// <summary>
    /// Ejecuta una acción asíncrona con reintentos automáticos en caso de excepción.
    /// </summary>
    /// <param name="action">Función asíncrona a ejecutar.</param>
    /// <param name="maxRetries">Número máximo de reintentos después del primer intento (>=0).</param>
    /// <param name="baseDelay">Tiempo base de espera entre intentos.</param>
    /// <param name="maxJitterMilliseconds">Máximo jitter en milisegundos para agregar aleatoriedad al delay (por defecto 100 ms).</param>
    /// <param name="cancellationToken">Token para cancelar la operación durante el delay.</param>
    /// <returns>Una tarea que completa cuando la acción se ejecuta con éxito o lanza excepción si se agotan los reintentos.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Si maxRetries es negativo o baseDelay es negativo.</exception>
    public static async Task ExecuteAsync(
        Func<Task> action,
        int maxRetries,
        TimeSpan baseDelay,
        int maxJitterMilliseconds = 100,
        CancellationToken cancellationToken = default)
    {
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), "maxRetries debe ser >= 0.");

        if (baseDelay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseDelay), "baseDelay no puede ser negativo.");

        if (maxJitterMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(maxJitterMilliseconds), "maxJitterMilliseconds no puede ser negativo.");

        int attempt = 0;

        while (true)
        {
            try
            {
                await action().ConfigureAwait(false);
                return;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                attempt++;

                // Jitter aleatorio entre 0 y maxJitterMilliseconds
                var jitter = TimeSpan.FromMilliseconds(_jitterer.Next(0, maxJitterMilliseconds + 1));

                // Backoff exponencial: baseDelay * 2^(attempt-1) + jitter
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)) + jitter;

                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Si el delay fue cancelado, re-lanzamos la cancelación
                    throw;
                }
            }
            catch
            {
                // Si no hay más reintentos, propagamos la excepción
                throw;
            }
        }
    }
}
