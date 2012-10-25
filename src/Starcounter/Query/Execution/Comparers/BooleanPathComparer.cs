// ***********************************************************************
// <copyright file="BooleanPathComparer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Query.Execution;
using System;
using System.Collections.Generic;

namespace Starcounter.Query.Execution
{
internal class BooleanPathComparer : BooleanComparer, IPathComparer
{
    internal BooleanPathComparer(BooleanProperty property, SortOrder ordering)
    : base(property, ordering)
    { }

    internal BooleanPathComparer(BooleanPath path, SortOrder ordering)
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
