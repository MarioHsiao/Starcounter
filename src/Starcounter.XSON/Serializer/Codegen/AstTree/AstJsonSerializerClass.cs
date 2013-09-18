// ***********************************************************************
// <copyright file="AstRequestProcessorClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Templates;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstJsonSerializerClass : AstBase {
        internal AstSerializeFunction SerializeFunction { get; set; }
        internal AstDeserializeFunction DeserializeFunction { get; set; }
        internal string ClassName { get; set; }
		internal string Namespace { get; set; }
		internal string Inherits { get; set; }
        internal ParseNode ParseNode { get; set; }

		internal string FullClassName {
			get {
				if (!string.IsNullOrEmpty(Namespace))
					return Namespace + "." + ClassName;
				return ClassName;
			}
		}

		internal override string DebugString {
			get {
				return "public " + ClassName + " : " + Inherits;
			}
		}
    }
}
