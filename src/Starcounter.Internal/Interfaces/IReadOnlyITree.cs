
using System.Collections.Generic;
namespace Starcounter.Internal {
    public interface IReadOnlyTree {
        IReadOnlyTree Parent {
            get;
        }
        IReadOnlyList<IReadOnlyTree> Children {
            get;
        }
    }
}
