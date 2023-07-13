using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Input.Virtual;

/// <summary>
/// Represents control scheme used for a specific part of your game, so both gameplay and UI could have their own respective control schemes. <br/>
/// Only one control scheme is processed at a time, making it useful for blocking input while typing and in other use cases.
/// </summary>
public interface IControlScheme {
    /// <summary>
    /// Enumerate all of the controls used by this scheme.
    /// </summary>
    IEnumerable<IVirtualControl> GetControls();
}
