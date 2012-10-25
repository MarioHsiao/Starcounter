// ***********************************************************************
// <copyright file="DateTimePathComparer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
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
