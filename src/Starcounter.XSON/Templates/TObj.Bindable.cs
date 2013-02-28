using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Advanced;

namespace Starcounter.Templates {
    public abstract class TObj<T> : TObj where T : IBindable {
    }
}
