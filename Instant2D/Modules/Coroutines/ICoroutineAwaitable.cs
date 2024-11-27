using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines;

/// <summary>
/// Provides a mechanism to extend awaitable actions during coroutines.
/// </summary>
public interface ICoroutineAwaitable {
    /// <summary>
    /// Called once when the awaitable is initially yielded.
    /// </summary>
    void Initialize(Coroutine coroutine);

    /// <summary>
    /// Called each coroutine tick. Return <see langword="true"/> to continue the coroutine.
    /// </summary>
    /// <returns> Whether or not coroutine should continue. </returns>
    bool Tick(Coroutine coroutine);
}