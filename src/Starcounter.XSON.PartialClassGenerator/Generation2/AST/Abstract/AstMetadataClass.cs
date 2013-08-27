// ***********************************************************************
// <copyright file="NMetadataClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Class NMetadataClass
    /// </summary>
    public class AstMetadataClass : AstInnerClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstMetadataClass(Gen2DomGenerator gen)
            : base(gen) {
        }

//        public override string ClassStemIdentifier {
//            get {
//                return HelperFunctions.GetClassStemIdentifier(NValueClass.NTemplateClass.Template.MetadataType);
//            }
//        }

        /// <summary>
        /// Uppers the first.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        public static string UpperFirst( string str ) {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        /// <summary>
        /// The instances
        /// </summary>
        public static Dictionary<TJson, AstClass> Instances = new Dictionary<TJson, AstClass>();

    }
}
