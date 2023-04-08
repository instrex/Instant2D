using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Modules;

/// <summary>
/// Game module interface implementing update and initialize functions.
/// </summary>
public interface IGameSystem : IComparable<IGameSystem> {
    int IComparable<IGameSystem>.CompareTo(Instant2D.Modules.IGameSystem other) => UpdateOrder.CompareTo(other.UpdateOrder);

    /// <summary>
    /// The order in which <see cref="Update(InstantApp, float)"/> functions are ran.
    /// </summary>
    float UpdateOrder { get; }

    /// <summary>
    /// Is called right before the game is ran and all game systems have been added in chronological order.
    /// </summary>
    void Initialize(InstantApp app);

    /// <summary>
    /// Is called each frame depending on framerate. 
    /// </summary>
    /// <param name="deltaTime"> Delta time since the last frame update. </param>
    void Update(InstantApp app, float deltaTime);
}
