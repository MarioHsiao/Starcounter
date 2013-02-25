// ***********************************************************************
// <copyright file="TContainer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.Templates {

    /// <summary>
    /// Base class for Obj and Arr templates.
    /// </summary>
    /// <remarks>
    /// Both arrays and objects can have children. Arrays has elements and objects has properties.
    /// In addition, the templates (TContainer) for this complex objects are frozen/sealed whenever there are
    /// instance Obj or Arr objects pertaining to them. This means that new templates need to be created to
    /// use alternate schemas.
    /// </remarks>
    public abstract class TContainer : TValue
    {
        /// <summary>
        /// <see cref="Sealed"/>
        /// </summary>
        private bool _Sealed;

        /// <summary>
        /// Once a TContainer (Obj or Arr schema) is in use (have instances), this property will return
        /// true and you cannot modify the template.
        /// </summary>
        /// <remarks>
        /// Exception should be change to an SCERR???? error.
        /// </remarks>
        /// <value><c>true</c> if sealed; otherwise, <c>false</c>.</value>
        /// <exception cref="System.Exception">Once a TObj is sealed, you cannot unseal it</exception>
        public override bool Sealed {
            get {
                return _Sealed;
            }
            internal set {
                if (!value && _Sealed) {
                    // TODO! SCERR!
                    throw new Exception("Once a TContainer (Obj or Arr schema) is in use (have instances), you cannot modify it");
                }
                _Sealed = value;
            }
        }

        /// <summary>
        /// Represents the contained properties (TObj) or the single contained type for typed arrays (TArr).
        /// </summary>
        /// <value>The child property or child element type template</value>
        public abstract IEnumerable<Template> Children { get; }

     }

}
