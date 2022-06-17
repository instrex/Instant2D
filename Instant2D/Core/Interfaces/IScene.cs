using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Core.Interfaces {
    public interface IScene<T> {
        void AddEntity(T entity);
    }
}
