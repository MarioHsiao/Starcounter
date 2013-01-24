// ***********************************************************************
// <copyright file="AstRequestProcessorClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal.Uri;
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

            Prefix.Add("");
            Prefix.Add("public static class " + ClassName + "{");
            Suffix.Add("}");
            Suffix.Add("");
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

        internal string AppClassName {
            get {
                ListingProperty listing;
                string name;
                AppTemplate at = (AppTemplate)((Template)ParseNode.AllHandlers[0].Code).Parent;

                name = at.ClassName;
                if (name == null) {
                    name = at.Name;
                    if (name == null) {
                        listing = at.Parent as ListingProperty;
                        if (listing != null)
                            name = listing.Name;
                        else
                            throw new Exception("Anonymous appclasses not supported for deserialization.");
                    }
                    name += "App";
                }
                return name;
            }
        }
    }
}
