
using Starcounter;
using Starcounter.Query.Execution;
using Sc.Server.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class DateTimePathComparer : DateTimeComparer, IPathComparer
{
    internal DateTimePathComparer(DateTimeProperty property, SortOrder ordering)
    : base(property, ordering)
    { }

    internal DateTimePathComparer(DateTimePath path, SortOrder ordering)
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
