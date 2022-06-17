using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Core {
    public abstract class SubSystem : IComparable<SubSystem> {
        public int CompareTo(SubSystem other) {
            return UpdateOrder.CompareTo(other.UpdateOrder);
        }

        /// <summary>
        /// Determines which systems will run first.
        /// </summary>
        public int UpdateOrder { get; set; }

        /// <summary>
        /// Happens right when all of the subsystems were added.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Happens each frame.
        /// </summary>
        public virtual void Update() { }

        
    }
}
