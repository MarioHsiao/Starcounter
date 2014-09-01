// ***********************************************************************
// <copyright file="NTApp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// The source code representation of the TApp class.
    /// </summary>
    public class AstSchemaClass : AstTemplateClass {

        /// <summary>
        /// Initializes a new instance of the <see cref="AstSchemaClass" /> class.
        /// </summary>
        public AstSchemaClass( Gen2DomGenerator gen ) : base( gen )
        {
            Constructor = new AstConstructor( gen ) { Parent = this };
        }

        /// <summary>
        /// The constructor
        /// </summary>
        public AstConstructor Constructor;

        /// <summary>
        /// If set to true all properties in this appclass will be automatically 
        /// bound, if not specified otherwise on the property, to the underlying dataobject in the app.
        /// </summary>
        public BindingStrategy BindChildren {
            get {
                var acn = (AstJsonClass)NValueClass;
                if (acn == null)
                    return BindingStrategy.Auto;
				if (acn.CodebehindClass == null) {
                    string value;
                    if (!((TObject)acn.NTemplateClass.Template).CodegenMetadata.TryGetValue(Gen2DomGenerator.InstanceDataTypeName, out value))
						return BindingStrategy.Auto;
					return BindingStrategy.Bound;
				}
				return acn.CodebehindClass.BindChildren;
            }
        }
    }
}
