using Instant2D.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public class Scene : IScene<Entity> {
        readonly List<Entity> _entities = new(100);
        readonly List<IUpdatable> _updatableEntities = new(50);

        public void AddEntity(Entity entity) {
            _entities.Add(entity);

            // register the entity as updatable
            if (entity is IUpdatable updatableEntity) {
                _updatableEntities.Add(updatableEntity);
            }

            
        }
    }
}
