using Microsoft.Xna.Framework;
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

        bool _useUpdate, _useRender;

        /// <summary> The Game instance this system is attached to. </summary>
        public InstantGame Game { get; init; }

        /// <summary> Determines which systems will run first. </summary>
        public int UpdateOrder { get; set; }

        /// <summary> When <see langword="true"/>, this system's <see cref="Update(GameTime)"/> will be run each frame. By default, this is set to <see langword="false"/>. </summary>
        public bool IsUpdatable {
            get => _useUpdate;
            set {
                if (_useUpdate == value)
                    return;

                // update the values
                _useUpdate = value;
                if (_useUpdate) {
                    Game._updatableSystems.Add(this);
                    Game._systemOrderDirty = true;
                } else {
                    Game._updatableSystems.Remove(this);
                }
            }
        }

        /// <summary> When <see langword="true"/>, <see cref="Render(GameTime)"/> will be called. Defaults to <see langword="false"/>. </summary>
        public bool IsRenderable {
            get => _useRender;
            set {
                if (_useRender == value)
                    return;

                _useRender = value;
                if (_useRender) {
                    Game._renderableSystems.Add(this);
                } else {
                    Game._renderableSystems.Remove(this);
                }
            }
        }

        /// <summary> Happens right when all of the subsystems were added. </summary>
        public virtual void Initialize() { }

        /// <summary> Happens each frame if <see cref="IsUpdatable"/> is set. </summary>
        public virtual void Update(GameTime time) { }

        /// <summary> Runs inside <see cref="Game.Draw(GameTime)"/> </summary>
        public virtual void Render(GameTime time) { }
    }
}
