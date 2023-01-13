using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public interface IPooled {
        /// <summary>
        /// Called after the object is returned into the pool. Make sure to null out all references and set default values here.
        /// </summary>
        void Reset();
    }
}
