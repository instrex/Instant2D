using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils.ResolutionScaling {
    public interface IResolutionScaler {
        /// <summary>
        /// Tranfsorm the screen dimensions to <see cref="ScaledResolution"/>.
        /// </summary>
        ScaledResolution Calculate(Point screenDimensions);
    }
}
