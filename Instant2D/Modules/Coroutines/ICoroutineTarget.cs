namespace Instant2D.Coroutines;

/// <summary>
/// Provides a special mechanism to define certain objects as "coroutine targets" and attach coroutines to them:
/// <list type="bullet">
/// <item> Coroutines will be automatically stopped when target is destroyed. </item> 
/// <item> <see cref="WaitForSeconds"/> will scale its duration based on <see cref="ICoroutineTarget.Timescale"/>, unless configured not to. </item> 
/// </list>
/// </summary>
public interface ICoroutineTarget {
    /// <summary>
    /// Current timescale value for this object.
    /// </summary>
    float Timescale { get; }
}
