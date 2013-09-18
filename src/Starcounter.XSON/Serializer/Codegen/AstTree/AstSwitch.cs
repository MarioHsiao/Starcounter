// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
	internal class AstSwitch : AstBase {
		internal ParseNode ParseNode { get; set; }

		internal override string DebugString {
			get {
				return "switch (i=" + ParseNode.MatchCharInTemplateAbsolute + ")";
			}
		}

		internal override bool IndentChildren {
			get {
				return true;
			}
		}
	}
}
