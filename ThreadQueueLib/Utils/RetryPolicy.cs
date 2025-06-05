namespace ThreadQueueLib.Utils;

/// <summary>
/// Proporciona una política simple de reintentos para ejecutar acciones asíncronas con posibilidad de reintentar en caso de fallo.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Ejecuta una acción asíncrona con reintentos automáticos en caso de excepción.
    /// </summary>
    /// <param name="action">La función asíncrona que se intentará ejecutar.</param>
    /// <param name="maxRetries">Número máximo de reintentos permitidos después del primer intento.</param>
    /// <param name="delay">Tiempo de espera entre cada intento de reintento.</param>
    /// <param name="cancellationToken">Token para cancelar la operación durante el delay.</param>
    /// <returns>Una tarea que completa cuando la acción se ejecuta con éxito o se agotan los reintentos.</returns>
    public static async Task ExecuteAsync(
        Func<Task> action,
        int maxRetries,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        // Intentar ejecutar la acción hasta maxRetries veces
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Intentar ejecutar la acción
                await action();
                return; // Si tuvo éxito, salir del método
            }
            catch when (attempt < maxRetries)
            {
                // Si hubo excepción y quedan intentos, esperar el delay antes del siguiente intento
                await Task.Delay(delay, cancellationToken);
            }
        }

        // Si agotó los intentos y sigue fallando, hacer un último intento (lanzará la excepción si falla)
        await action();
    }
}
