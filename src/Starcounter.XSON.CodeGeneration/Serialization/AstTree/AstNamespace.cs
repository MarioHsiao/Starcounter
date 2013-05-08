﻿// ***********************************************************************
// <copyright file="AstNamespace.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Class AstNamespace
    /// </summary>
    public class AstNamespace : AstNode {
        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        /// <value>The namespace.</value>
        public string Namespace { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "namespace " + Namespace;
            }
        }

        /// <summary>
        /// Generates the C# code header (namespace and using statements)
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            Prefix.Add("// Generated code. This code serializes and deserializes Typed Json. The code was generated by Starcounter.");
            Prefix.Add("");
            Prefix.Add("using System;");
            Prefix.Add("using Starcounter;");
            Prefix.Add("using Starcounter.Internal;");
            Prefix.Add("using Starcounter.Internal.JsonPatch;");
            Prefix.Add("");
            var sb = new StringBuilder();
            sb.Append("namespace ");
            sb.Append(Namespace);
            sb.Append(" {");
            Prefix.Add(sb.ToString());
            Suffix.Add("}");
        }
    }
}
