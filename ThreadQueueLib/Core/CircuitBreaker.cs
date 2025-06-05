namespace ThreadQueueLib.Core;

public class CircuitBreaker(int maxFailures, TimeSpan openDuration)
{
    private readonly int _maxFailures = maxFailures;
    private readonly TimeSpan _openDuration = openDuration;
    private int _failureCount = 0;
    private DateTime? _openUntil;

    /// <summary>
    /// Indica si el circuito está abierto (no permitir más ejecuciones).
    /// </summary>
    public bool IsOpen => _openUntil.HasValue && DateTime.UtcNow < _openUntil.Value;

    /// <summary>
    /// Resetea el contador al detectar éxito.
    /// </summary>
    public void OnSuccess()
    {
        _failureCount = 0;
        _openUntil = null;
    }

    /// <summary>
    /// Incrementa contador y abre circuito si supera el máximo.
    /// </summary>
    public void OnFailure()
    {
        _failureCount++;
        if (_failureCount >= _maxFailures)
            _openUntil = DateTime.UtcNow.Add(_openDuration);
    }
}