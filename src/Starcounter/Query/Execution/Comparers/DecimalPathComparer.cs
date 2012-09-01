
using Starcounter;
using Starcounter.Query.Execution;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class DecimalPathComparer : DecimalComparer, IPathComparer
{
    internal DecimalPathComparer(DecimalProperty property, SortOrder ordering)
    : base(property, ordering)
    { }

    internal DecimalPathComparer(DecimalPath path, SortOrder ordering)
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
