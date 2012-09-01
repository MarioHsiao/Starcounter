
using Starcounter.Internal;
using System;

namespace Starcounter
{

    [Flags]
    public enum RangeFlags : uint
    {

        ValidLesserKey = sccoredb.SC_ITERATOR_RANGE_VALID_LSKEY,

        IncludeLesserKey = sccoredb.SC_ITERATOR_RANGE_INCLUDE_LSKEY,

        ValidGreaterKey = sccoredb.SC_ITERATOR_RANGE_VALID_GRKEY,

        IncludeGreaterKey = sccoredb.SC_ITERATOR_RANGE_INCLUDE_GRKEY,
    }
}
