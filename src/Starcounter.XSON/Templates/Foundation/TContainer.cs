// ***********************************************************************
// <copyright file="TContainer.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.DeserializerCompiler;

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
    public abstract class TContainer : TValue {
		/// <summary>
		/// 
		/// </summary>
        public override bool IsPrimitive {
            get { return false; }
        }

		internal abstract Json GetValue(Json parent);

        /// <summary>
        /// Represents the contained properties (TObj) or the single contained type for typed arrays (TArr).
        /// </summary>
        /// <value>The child property or child element type template</value>
        public abstract IEnumerable<Template> Children { get; }

        internal void UpdateParentAndIndex(Json parent, Json newValue) {
            if (newValue != null) {
                if (newValue.Parent != parent)
                    newValue.Parent = parent;
                newValue.cacheIndexInArr = TemplateIndex;
            }

            var oldValue = (Json)GetUnboundValueAsObject(parent);
            if (oldValue != null) {
                oldValue.SetParent(null);
                oldValue.cacheIndexInArr = -1;
            }
        }
    }
}
