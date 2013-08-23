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
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassStemIdentifier {
            get {
                if (MatchedClass != null) {
#if DEBUG
                    if (MatchedClass.ClassName.Contains("<"))
                        throw new Exception();
                    if (MatchedClass.ClassName.Contains("."))
                        throw new Exception();
#endif
                    return "CodeBehind" + MatchedClass.ClassName;
                }
                var acn = ((AstJsonClass)NValueClass);
#if DEBUG
                if (acn.ClassStemIdentifier.Contains("<"))
                    throw new Exception();
//                if (Template == Generator.DefaultObjTemplate) {
//                    return HelperFunctions.GetClassStemIdentifier(Template.GetType());
//                }
#endif
      //          return "Schema";
                return base.ClassStemIdentifier;
            }
        }



        /// <summary>
        /// The constructor
        /// </summary>
        public AstConstructor Constructor;

        /// <summary>
        /// If set to true all properties in this appclass will be automatically 
        /// bound, if not specified otherwise on the property, to the underlying dataobject in the app.
        /// </summary>
        public bool AutoBindProperties {
            get {
                var acn = (AstJsonClass)NValueClass;
                if (acn == null || acn.MatchedClass == null)
                    return false;
                return acn.MatchedClass.AutoBindToDataObject;
            }
        }
    }
}
