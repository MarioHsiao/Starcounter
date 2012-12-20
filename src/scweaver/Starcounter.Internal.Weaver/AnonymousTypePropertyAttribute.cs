// ***********************************************************************
// <copyright file="AnonymousTypePropertyAttribute.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Class AnonymousTypePropertyAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class AnonymousTypePropertyAttribute : Attribute {
        /// <summary>
        /// The index
        /// </summary>
        private int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousTypePropertyAttribute" /> class.
        /// </summary>
        /// <param name="index">The index.</param>
        public AnonymousTypePropertyAttribute(int index) {
            this.index = index;
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>The index.</value>
        public int Index {
            get {
                return this.index;
            }
        }
    }
}