using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter {

    [Database]
    public abstract class Entity2 : IEntity {

        public abstract void OnDelete();
    }
}
