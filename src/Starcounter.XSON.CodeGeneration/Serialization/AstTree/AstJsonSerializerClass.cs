// ***********************************************************************
// <copyright file="AstRequestProcessorClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstRequestProcessorClass
    /// </summary>
    internal class AstJsonSerializerClass : AstNode {
        private List<AstSerializeFunction> serializers = new List<AstSerializeFunction>();

        /// <summary>
        /// Gets the list of serializers for this class (main + child serializers)
        /// </summary>
        /// <value></value>
        internal AstSerializeFunction SerializeFunction { get; set; }

        /// <summary>
        /// Gets or sets the deserializer function.
        /// </summary>
        /// <value></value>
        internal AstDeserializeFunction DeserializeFunction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the parse node.
        /// </summary>
        /// <value>The parse node.</value>
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Gets the debug string.
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "public class " + ClassName;
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            Prefix.Add("public class " + ClassName + "{");
            Suffix.Add("}");
        }
    }
}
