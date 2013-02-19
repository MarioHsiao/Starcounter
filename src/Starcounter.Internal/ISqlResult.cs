using System.Collections;
using System.Collections.Generic;

namespace Starcounter {

    public interface ISqlResult : IEnumerable {
        dynamic First { get; }
    }
            
    public interface ISqlResult<T> : ISqlResult, IEnumerable<T> {
        new T First { get; }
    }
}
