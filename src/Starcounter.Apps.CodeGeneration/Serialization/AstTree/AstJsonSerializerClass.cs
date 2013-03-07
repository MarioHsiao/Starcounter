// ***********************************************************************
// <copyright file="AstRequestProcessorClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class AstRequestProcessorClass
    /// </summary>
    internal class AstJsonSerializerClass : AstNode {
        /// <summary>
        /// Gets or sets the serializer function.
        /// </summary>
        /// <value></value>
        internal AstSerializeFunction SerializeFunction { get; set; }

        /// <summary>
        /// Gets or sets the deserializer function.
        /// </summary>
        /// <value></value>
        internal AstDeserializeFunction DeserializeFunction { get; set; }

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
            Prefix.Add("public static class " + ClassName + "{");
            Suffix.Add("}");
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        internal string ClassName {
            get {
                return AppClassName + "JsonSerializer";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal string AppClassName {
            get {
                TPuppet at = (TPuppet)((Template)ParseNode.AllHandlers[0].Code).Parent;
                return AstTreeHelper.GetAppClassName(at);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal string FullAppClassName {
            get {
                TPuppet at = (TPuppet)((Template)ParseNode.AllHandlers[0].Code).Parent;
                return AstTreeHelper.GetFullAppClassName(at);
            }
        }
    }
}
