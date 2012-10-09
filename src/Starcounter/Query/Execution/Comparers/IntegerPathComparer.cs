
using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class IntegerPathComparer : IntegerComparer, IPathComparer
{
    internal IntegerPathComparer(IntegerProperty property, SortOrder ordering)
    : base(property, ordering)
    { }

    internal IntegerPathComparer(IntegerPath path, SortOrder ordering)
    : base(path, ordering)
    { }

    public IPath Path
    {
        get
        {
            return (expression as IPath);
        }
    }
}
}
