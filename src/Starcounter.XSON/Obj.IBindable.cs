// ***********************************************************************
// <copyright file="Obj.IBindable.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using Starcounter.Advanced;

namespace Starcounter {
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Obj<T> : Obj where T : IBindable {

        /// <summary>
        /// </summary>
        /// <value></value>
        //public new T Data 
        //{
        //    get { return (T)base.Data; }
        //    set { base.Data = value; }
        //}
    }
}