using System;
using Starcounter.Templates;
using Starcounter.XSON.Serializer.Ast;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer {
	internal class StdDomGenerator {
		private AstRoot domTree;
		private TObject template;

		internal StdDomGenerator(TObject template) {
			this.template = template;
		}

		internal AstRoot GenerateDomTree() {
			ParseNode parseTree = ParseTreeGenerator.BuildParseTree(template);

            string ns = null;//template.Namespace;
			if (String.IsNullOrEmpty(ns)) {
				ns = "__starcountergenerated__";
			}

			AstRoot root = new AstRoot();

			string className = template.ClassName;
			if (String.IsNullOrEmpty(className))
				className = template.PropertyName;

			AstJsonSerializerClass jsClass = BuildAstTree(parseTree, template.ClassName + "Serializer");
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

		///// <summary>
		///// Creates serializers for each property for objects defined by an
		///// object schema.
		///// </summary>
		///// <param name="objTemplate"></param>
		///// <param name="parent"></param>
		//private static void CreateChildSerializers(TObject objTemplate, AstNode parent) {
		//	AstNode node;
		//	TObject tChildObj;
		//	string className;

		//	foreach (Template child in objTemplate.Properties) {
		//		tChildObj = null;
		//		if (child is TObject) {
		//			tChildObj = (TObject)child;
		//		} else if (child is TObjArr) {
		//			tChildObj = ((TObjArr)child).ElementType;
		//		}

		//		if (tChildObj != null) {
		//			className = tChildObj.ClassName;
		//			if (string.IsNullOrEmpty(className))
		//				className = tChildObj.PropertyName;
		//			node = BuildAstTree(ParseTreeGenerator.BuildParseTree(tChildObj), className + "Serializer");
		//			node.Parent = parent;
		//			CreateChildSerializers(tChildObj, node);
		//		}
		//	}
		//}

		/// <summary>
		/// Creates the json serializer class.
		/// </summary>
		/// <param name="input">The input.</param>
		/// <returns>An AstRequestProcessorClass node.</returns>
		private AstJsonSerializerClass BuildAstTree(ParseNode input, string className) {
			AstJsonSerializerClass jsClass = new AstJsonSerializerClass() {
				ParseNode = input,
				ClassName = className,
				Inherits = "StandardJsonSerializerBase"
			};

			var cons = new AstJSConstructor() {
				Parent = jsClass,
				ParseNode = input
			};

			// Removed the serializer function since the hardcoded one is almost as fast and even
			// faster in certain circumstances.
			//var serializeFunc = CreateSerializerFunction(input);
			//jsClass.SerializeFunction = serializeFunc;
			//serializeFunc.Parent = jsClass;

			var deserializeFunc = CreateDeserializerFunction(input);
			jsClass.DeserializeFunction = deserializeFunc;
			deserializeFunc.Parent = jsClass;

			return jsClass;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		private AstSerializeFunction CreateSerializerFunction(ParseNode input) {
			Template template;
			AstBase nextParent;

			var sf = new AstSerializeFunction();
			nextParent = sf;

			//var astUnsafe = new AstUnsafe() {
			//	Parent = nextParent
			//};

			//nextParent = new AstLabel() {
			//	Parent = nextParent,
			//	Label = "restart"
			//};

			//new AstRecreateBuffer() {
			//	Parent = nextParent
			//};

			//nextParent = new AstFixed() {
			//	Parent = nextParent
			//};

			//var astObj = new AstJsonObject() {
			//	Parent = nextParent
			//};

			for (int i = 0; i < input.AllTemplates.Count; i++) {
				template = input.AllTemplates[i].Template;

				//nextParent = new AstCheckAlreadyProcessed() {
				//	Index = i,
				//	Parent = astObj
				//};

				new AstJsonProperty() {
					Template = template,
					Parent = nextParent,
					IsLast = ((i + 1) == input.AllTemplates.Count)
				};

				//if (template is TObject) {
				//	new AstJsonObjectValue() {
				//		Template = template,
				//		Parent = nextParent
				//	};
				//} else if (template is TObjArr) {
				//	new AstJsonObjectArrayValue() {
				//		Template = template,
				//		Parent = nextParent
				//	};
				//} else {
				//	new AstJsonPropertyValue() {
				//		Template = template,
				//		Parent = nextParent
				//	};
				//}

				//if ((i + 1) < input.AllTemplates.Count) {
				//	new AstJsonDelimiter() {
				//		Delimiter = ',',
				//		Parent = nextParent
				//	};
				//}
			}

			//new AstReturn() {
			//	Parent = sf
			//};

			return sf;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="input"></param>
		private AstDeserializeFunction CreateDeserializerFunction(ParseNode input) {
			AstBase nextParent;

			var df = new AstDeserializeFunction();
			nextParent = df;

			nextParent = new AstWhile() {
				Parent = nextParent
			};

			new AstGotoProperty() {
				Parent = nextParent
			};

			if (input.Candidates.Count > 0 && input.Candidates[0].DetectedType == NodeType.CharMatchNode) {
				CreateVerificationNode(nextParent, input.Candidates[0]);

				nextParent = new AstSwitch() {
					ParseNode = input.Candidates[0],
					Parent = nextParent
				};
			}

			foreach (var cand in input.Candidates) {
				CreateCodeNode(cand, nextParent);
			}

			return df;
		}

		/// <summary>
		/// Creates the code node.
		/// </summary>
		/// <param name="pn">The pn.</param>
		/// <param name="parent">The parent.</param>
		private void CreateCodeNode(ParseNode pn, AstBase parent) {
			AstBase nextParent;
			int indentation = 4;

			switch (pn.DetectedType) {
				case NodeType.CharMatchNode:
					var cn = new AstCase();
					cn.Parent = parent;
					cn.ParseNode = pn;
					nextParent = cn;

					if (pn.Candidates.Count > 1) {
						CreateVerificationNode(cn, pn.Candidates[0]);

						var nsn = new AstSwitch() {
							Parent = cn,
							ParseNode = pn.Candidates[0]
						};
						nextParent = nsn;
					} else {
						nextParent = cn;
					}
					foreach (var cand in pn.Candidates) {
						CreateCodeNode(cand, nextParent);
					}

					break;
				case NodeType.Heureka:
					nextParent = parent;
					CreateVerificationNode(nextParent, pn);

					new AstGotoValue() {
						Parent = nextParent
					};

					if (pn.Template.Template is TObjArr)
						indentation = 8;

					new AstJsonProperty() {
						Template = pn.Template.Template,
						Indentation = indentation,
						ParseNode = pn,
						Parent = nextParent
					};

					//if (pn.Template.Template is TObjArr) {
					//	// If the value to parse is a list we need to add some additional 
					//	// code for looping and checking end of array.
					//	nextParent = new AstParseJsonObjectArray() {
					//		Parent = nextParent,
					//		Template = pn.Template.Template
					//	};
					//	indentation = 8;
					//}

					//var pj = new AstParseJsonValue() {
					//	ParseNode = pn,
					//	Parent = nextParent,
					//	Indentation = indentation
					//};
					break;
				default:
					//new AstError() {
					//	Parent = parent,
					//	ParseNode = pn
					//};
					break;
			}
		}

		private void CreateVerificationNode(AstBase parent, ParseNode node) {
			if (!node.TagAsVerified) {
				int vlen = node.MatchCharInTemplateAbsolute;
				if (vlen > 0) {
					new AstVerifier() {
						Parent = parent,
						ParseNode = node
					};
				}
				node.TagAsVerified = true;
			}
		}
	}
}
