// ***********************************************************************
// <copyright file="AstSwitch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;

namespace Starcounter.XSON.Serializer.Ast {
	internal class AstWhile : AstBase {
		internal override string DebugString {
			get {
				return "while (not end of buffer)";
			}
		}
	}
}
