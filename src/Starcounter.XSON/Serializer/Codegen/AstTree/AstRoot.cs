using System;

namespace Starcounter.XSON.Serializer.Ast {
	internal class AstRoot : AstBase {
		internal AstJsonSerializerClass SerializerClass {
			get { return Children[0] as AstJsonSerializerClass; }
		}
	}
}
