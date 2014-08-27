
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.TObject;



namespace Starcounter.Internal.MsBuild.Codegen {
    public partial class Gen2DomGenerator {
        private AstJsonClass DefaultJson;
		private AstInstanceClass DefaultJsonArray;

        public AstJsonClass GetDefaultJson() {
            if (DefaultJson == null) {
                DefaultJson = new AstJsonClass(this) {
                    GlobalClassSpecifier = "global::Starcounter.Json",
                    ClassStemIdentifier = "Json"
                };
                DefaultJson.NTemplateClass = new AstSchemaClass(this) {
                    NValueClass = DefaultJson,
                    Template = DefaultObjTemplate,
                    GlobalClassSpecifier = "global::Starcounter.Json.JsonByExample.Schema",
                    ClassStemIdentifier = "Schema"
                };
                DefaultJson.NMetadataClass = new AstJsonMetadataClass(this) {
                    NValueClass = DefaultJson,
                    GlobalClassSpecifier = "global::Starcounter.Json.JsonByExample.Metadata<global::Starcounter.Json.JsonByExample.Schema",
                    ClassStemIdentifier = "Metadata"
                };
            }
            return DefaultJson;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public AstInstanceClass GetDefaultJsonArrayClass() {
			if (DefaultJsonArray == null) {
				DefaultJsonArray = new AstInstanceClass(this);
				var json = GetDefaultJson();

				DefaultJsonArray.Generic = new AstClass[] {
					json
				};
				DefaultJsonArray.ClassStemIdentifier = "Arr";
				DefaultJsonArray.Namespace = "Starcounter";

				DefaultJsonArray.NTemplateClass = new AstTemplateClass(this) {
					NValueClass = DefaultJsonArray,
					ClassStemIdentifier = "TArray",
					Namespace = "Starcounter.Templates",
					Generic = new AstClass[] {
						json
					},
					Template = DefaultObjTemplate
				};

				DefaultJsonArray.NMetadataClass = new AstMetadataClass(this) {
					NValueClass = DefaultJsonArray,
					ClassStemIdentifier = "Metadata"
				};
			}
			return DefaultJsonArray;
		}

        public AstInstanceClass GetJsonArrayClass(string instanceTypeName) {
            var astArray = new AstInstanceClass(this);
            var genericTypeClass = new AstJsonClass(this) {  ClassStemIdentifier = instanceTypeName, ParentProperty = astArray };
            genericTypeClass.CodebehindClass = new Starcounter.XSON.Metadata.CodeBehindClassInfo(null) { ClassName = instanceTypeName };

            astArray.Generic = new AstClass[] { genericTypeClass };
            astArray.ClassStemIdentifier = "Arr";
            astArray.Namespace = "Starcounter";

            astArray.NTemplateClass = new AstTemplateClass(this) {
                NValueClass = astArray,
                ClassStemIdentifier = "TArray",
                Namespace = "Starcounter.Templates",
                Generic = new AstClass[] { genericTypeClass },
            };

            astArray.NMetadataClass = new AstMetadataClass(this) {
                NValueClass = astArray,
                ClassStemIdentifier = "Metadata"
            };
            return astArray;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstInstanceClass> ValueClasses = new Dictionary<Template, AstInstanceClass>();

        /// <summary>
        /// </summary>
        public Dictionary<Template, AstTemplateClass> TemplateClasses = new Dictionary<Template, AstTemplateClass>();

        /// <summary>
        /// </summary>
        public Dictionary<Type, AstTemplateClass> TemplateClassesByType = new Dictionary<Type, AstTemplateClass>();

        /// <summary>
        /// </summary>
        public Dictionary<Template, AstMetadataClass> MetaClasses = new Dictionary<Template, AstMetadataClass>();




        internal TString TPString = new TString();
        internal TLong TPLong = new TLong();
        internal TDecimal TPDecimal = new TDecimal();
        internal TJson DefaultObjTemplate = null;
        internal TDouble TPDouble = new TDouble();
        internal TBool TPBool = new TBool();
        internal TTrigger TPAction = new TTrigger();

        /// <summary>
        /// Gets the prototype.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Template.</returns>
        public Template GetPrototype(Template template) {
            if (template is TString) {
                return TPString;
            }
            else if (template is TLong) {
                return TPLong;
            }
            else if (template is TDouble) {
                return TPDouble;
            }
            else if (template is TDecimal) {
                return TPDecimal;
            }
            else if (template is TBool) {
                return TPBool;
            }
            else if (template is TTrigger) {
                return TPAction;
            }
            return template;
        }


        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public AstInstanceClass ObtainValueClass(Template template) {
            //            if (template is Schema<Json>) {
            //                var acn = new AstAppClass(this) {
            //
            //                };
            //            }

            AstInstanceClass ret;
            if (template.IsPrimitive) {
                template = GetPrototype(template);
            }
            if (ValueClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                ret = new AstPrimitiveType(this);
                ValueClasses.Add(template, ret);
                ret.BuiltInType = template.InstanceType;
//                var ns = template.GetType().Namespace;
//                string nsa;
//                if (ns == "Starcounter")
//                    nsa = "s::";
//                else
//                    nsa = "st::";
//                ret.NamespaceAlias = ;
//                ret.Namespace = ns;
                ret.NTemplateClass = ObtainTemplateClass(template);

                ret.NMetadataClass = ObtainMetaClass(template);
                return ret;
            }

            if (template is TObject) {
                var tjson = template as TObject;
                var acn = new AstJsonClass(this);
                ValueClasses.Add(template, acn);
                acn.InheritedClass = GetDefaultJson();
                acn.Namespace = template.Namespace;
                var jsonbyexample = new AstOtherClass(this) {
                    Parent = acn,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true
                };
                acn.NJsonByExample = jsonbyexample;
                if (template.Parent != null) {
                    acn.ParentProperty = (AstInstanceClass)ObtainValueClass(template.Parent);
                }

//                if (template == DefaultObjTemplate) {
//                    acn.NamespaceAlias = "s::";
//                    acn.Generic = new AstClass[] {
//                        AstObject
//                    };
//                }
                acn.NMetadataClass = ObtainMetaClass(template);
                acn.NTemplateClass = ObtainTemplateClass(template);
                //var className = GenerateJsonClassName(acn);

                //acn.ClassStemIdentifier = className;
                return acn;
            }
            else if (template is TObjArr) {
                var tarr = template as TObjArr;
                var acn = new AstInstanceClass(this);
                ValueClasses.Add(template, acn);
                acn.NMetadataClass = ObtainMetaClass(template);
                acn.NTemplateClass = ObtainTemplateClass(template);


                var newJson = ObtainValueClass(tarr.ElementType);
                newJson.Parent = ObtainValueClass(template.Parent);
    
//                acn.BuiltInType = template.InstanceType;
                acn.Namespace = template.InstanceType.Namespace;
                acn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(template.InstanceType );
                acn.Generic = new AstClass[] {
                        newJson
                    };
                return acn;
            }
            throw new Exception();
        }

        /// <summary>
        /// Finds or creates a metadata node (class declaration/type) for a
        /// given property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NMetadataClass.</returns>
        public AstMetadataClass ObtainMetaClass(Template template) {
            //  template = GetPrototype(template);
            if (template.IsPrimitive) {
                template = GetPrototype(template);
            }

            AstMetadataClass ret;
            if (MetaClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                ret = new AstMetadataClass(this);
                MetaClasses.Add(template, ret);
/*                var ns = template.GetType().Namespace;
                string nsa;
                if (ns == "Starcounter")
                    nsa = "s::";
                else
                    nsa = "st::";
                ret.NamespaceAlias = nsa;
 */
                ret.BuiltInType = template.MetadataType;

                ret.NValueClass = ObtainValueClass(template);
                return ret;
            }


            AstInstanceClass parent = null;
            if (template.Parent != null)
                parent = ObtainValueClass(template.Parent);
            else
                parent = this.GetDefaultJson();

            if (template is TObject) {
                AstClass[] gen;
                gen = new AstClass[] {
                    ObtainTemplateClass(template),
                    parent
                };
                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };
                MetaClasses.Add(template, mcn);
                var acn = (AstJsonClass)ObtainValueClass(template);
                mcn.NValueClass = acn;
                mcn.InheritedClass = ((AstJsonClass)acn.InheritedClass).NMetadataClass;
                mcn.Namespace = template.Namespace;

                
                // TODO! Add back
                //  mcn.Parent = acn.NJsonByExample;

                mcn.ClassStemIdentifier = "Metadata";
//                mcn.GlobalClassSpecifier = HelperFunctions.GetGlobalClassSpecifier(template.MetadataType, true);

//                if (template == DefaultObjTemplate) {
//                    mcn.NamespaceAlias = "st::";
//                }
                return mcn;
            }
            else if (template is TObjArr) {
                var tarr = template as TObjArr;
                AstClass[] gen;
                gen = new AstClass[] {
                    ObtainTemplateClass(tarr.ElementType),
                    parent
                };
                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };
                MetaClasses.Add(template, mcn);

                var tarrType = tarr.GetType();
                mcn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(tarrType);
                mcn.Namespace = tarrType.Namespace;

                mcn.NValueClass = ObtainValueClass(template);
                return mcn;
            }
            else {
                AstClass[] gen;
                gen = new AstClass[] {
                    parent
                };
                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };
                MetaClasses.Add(template, mcn);
                mcn.NValueClass = ObtainValueClass(template);
                return mcn;
            }
        }


        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NTemplateClass.</returns>
        public AstTemplateClass ObtainTemplateClass(Template template) {

            if (template.IsPrimitive) {
                template = GetPrototype(template);
            }

            AstTemplateClass ret;
            if (TemplateClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                ret = new AstTemplateClass(this) {
                    Template = template,
                };
                TemplateClasses.Add(template, ret);
                var type = template.GetType();
/*                var ns = type.Namespace;
                string nsa;
                if (ns == "Starcounter")
                    nsa = "s::";
                else
                    nsa = "st::";
                ret.NamespaceAlias = nsa;
 */
                ret.BuiltInType = type;
 //               ret.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(type);
                ret.NValueClass = ObtainValueClass(template);
                return ret;
            }


            if (template is TObject) {
                ret = new AstSchemaClass(this) {
                    Template = template
                };
                TemplateClasses.Add(template, ret);
                var acn = (AstJsonClass)ObtainValueClass(template);
                ret.InheritedClass = ((AstJsonClass)acn.InheritedClass).NTemplateClass;
                ret.Namespace = template.Namespace;
                ret.NValueClass = acn;
                ret.Parent = acn.NJsonByExample;
                ret.ClassStemIdentifier = "Schema";
//                ret.GlobalClassSpecifier = HelperFunctions.GetGlobalClassSpecifier(type,true);
//                if (type.IsNested) {
//                    ret.Namespace += "." + HelperFunctions.GetClassStemIdentifier(type.DeclaringType);
//                }
              //  if (template == DefaultObjTemplate) {
              //      ret.NamespaceAlias = "st::";
              //  }
//                ret.InheritedClass = ObtainTemplateClass(DefaultObjTemplate);

//                var acn = this.ObtainValueClass(template);

                //                ret.Generic = new AstClass[] { acn };

                ret.NValueClass = acn;
                return ret;
            }
            if (template is TObjArr) {
                var tarr = template as TObjArr;
                ret = new AstTemplateClass(this) {
                    Template = template
                };
                TemplateClasses.Add(template, ret);
                ret.NValueClass = ObtainValueClass(template);
                var acn = ObtainValueClass(tarr.ElementType);
                var tarrType = tarr.GetType();
                ret.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(tarrType);
//                ret.NamespaceAlias = "st::";
                ret.Namespace = tarrType.Namespace;
                ret.Generic = new AstClass[] { acn };
                return ret;
            }
            throw new Exception();
        }

    }
}
