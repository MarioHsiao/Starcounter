using System;
using Starcounter.Templates;
using Starcounter.XSON.Serializer.Ast;

namespace Starcounter.XSON.Serializer {
	internal class FTJDomGenerator {
		private AstRoot domTree;
		private TObject template;

		internal FTJDomGenerator(TObject template) {
			this.template = template;
		}

		internal AstRoot GenerateDomTree() {
            string ns = null;//template.Namespace;
			if (String.IsNullOrEmpty(ns)) {
				ns = "__starcountergenerated__";
			}

			AstRoot root = new AstRoot();

			string className = template.ClassName;
			if (String.IsNullOrEmpty(className))
				className = template.PropertyName;

			AstJsonSerializerClass jsClass = BuildAstTree(template.ClassName + "FTJSerializer");
			jsClass.Parent = root;
			jsClass.Namespace = ns;

			domTree = root;
			return root;
		}

		internal AstRoot DomTree {
			get { return domTree; }
		}

		internal TObject Template {
			get { return template; }
		}

		/// <summary>
		/// Creates the json serializer class.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>An AstRequestProcessorClass node.</returns>
		private AstJsonSerializerClass BuildAstTree(string className) {
			AstJsonSerializerClass jsClass = new AstJsonSerializerClass() {
				ClassName = className,
				Inherits = "FasterThanJsonSerializer"
			};

			// Removed the serializer function since the hardcoded one is almost as fast and even
			// faster in certain circumstances.
			//var serializeFunc = CreateSerializerFunction(input);
			//jsClass.SerializeFunction = serializeFunc;
			//serializeFunc.Parent = jsClass;

			var deserializeFunc = CreateDeserializerFunction();
			jsClass.DeserializeFunction = deserializeFunc;
			deserializeFunc.Parent = jsClass;

			return jsClass;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		private AstDeserializeFunction CreateDeserializerFunction() {
			AstBase nextParent;

			var df = new AstDeserializeFunction();
			nextParent = df;

			foreach (Template child in this.Template.Properties.ExposedProperties) {
				new AstJsonProperty() {
					Template = child,
					Parent = nextParent
				};
			}
			return df;
		}

		///// <summary>
		///// 
		///// </summary>
		///// <param name="input"></param>
		//private AstSerializeFunction CreateSerializerFunction() {
		//	Template template;
		//	AstBase nextParent;

		//	var sf = new AstSerializeFunction();
		//	nextParent = sf;

		//	//var astUnsafe = new AstUnsafe() {
		//	//	Parent = nextParent
		//	//};

		//	//nextParent = new AstLabel() {
		//	//	Parent = nextParent,
		//	//	Label = "restart"
		//	//};

		//	//new AstRecreateBuffer() {
		//	//	Parent = nextParent
		//	//};

		//	//nextParent = new AstFixed() {
		//	//	Parent = nextParent
		//	//};

		//	//var astObj = new AstJsonObject() {
		//	//	Parent = nextParent
		//	//};

		//	for (int i = 0; i < input.AllTemplates.Count; i++) {
		//		template = input.AllTemplates[i].Template;

		//		//nextParent = new AstCheckAlreadyProcessed() {
		//		//	Index = i,
		//		//	Parent = astObj
		//		//};

		//		new AstJsonProperty() {
		//			Template = template,
		//			Parent = nextParent,
		//			IsLast = ((i + 1) == input.AllTemplates.Count)
		//		};

		//		//if (template is TObject) {
		//		//	new AstJsonObjectValue() {
		//		//		Template = template,
		//		//		Parent = nextParent
		//		//	};
		//		//} else if (template is TObjArr) {
		//		//	new AstJsonObjectArrayValue() {
		//		//		Template = template,
		//		//		Parent = nextParent
		//		//	};
		//		//} else {
		//		//	new AstJsonPropertyValue() {
		//		//		Template = template,
		//		//		Parent = nextParent
		//		//	};
		//		//}

		//		//if ((i + 1) < input.AllTemplates.Count) {
		//		//	new AstJsonDelimiter() {
		//		//		Delimiter = ',',
		//		//		Parent = nextParent
		//		//	};
		//		//}
		//	}

		//	//new AstReturn() {
		//	//	Parent = sf
		//	//};

		//	return sf;
		//}
	}
}
