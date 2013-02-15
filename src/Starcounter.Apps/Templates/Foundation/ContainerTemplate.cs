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
        /// Once a ContainerTemplate (Obj or Arr schema) is in use (have instances), this property will return
        /// true and you cannot modify the template.
        /// </summary>
        /// <remarks>
        /// Exception should be change to an SCERR???? error.
        /// </remarks>
        /// <value><c>true</c> if sealed; otherwise, <c>false</c>.</value>
        /// <exception cref="System.Exception">Once a ObjTemplate is sealed, you cannot unseal it</exception>
        public override bool Sealed {
            get {
                return _Sealed;
            }
            internal set {
                if (!value && _Sealed) {
                    // TODO! SCERR!
                    throw new Exception("Once a ContainerTemplate (Obj or Arr schema) is in use (have instances), you cannot modify it");
                }
                _Sealed = value;
            }
        }

        /// <summary>
        /// Represents the contained properties (ObjTemplate) or the single contained type for typed arrays (ArrProperty).
        /// </summary>
        /// <value>The child property or child element type template</value>
        public abstract IEnumerable<Template> Children { get; }

     }

}
