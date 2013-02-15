// ***********************************************************************
// <copyright file="ParentTemplate.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif


    /// <summary>
    /// Base class for Obj and Arr templates.
    /// </summary>
    public abstract class ContainerTemplate : Property
    {
        /// <summary>
        /// The _ sealed
        /// </summary>
        private bool _Sealed;

        /// <summary>
        /// If this property returns true, you are not allowed to alter the properties of this template.
        /// </summary>
        /// <value><c>true</c> if sealed; otherwise, <c>false</c>.</value>
        /// <exception cref="System.Exception">Once a ObjTemplate is sealed, you cannot unseal it</exception>
        public override bool Sealed {
            get {
                return _Sealed;
            }
            internal set {
                if (!value && _Sealed) {
                    throw new Exception("Once a ObjTemplate is sealed, you cannot unseal it");
                }
                _Sealed = value;
            }
        }

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public abstract IEnumerable<Template> Children { get; }

     }

}
