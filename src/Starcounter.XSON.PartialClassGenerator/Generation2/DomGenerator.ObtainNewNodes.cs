
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;



namespace Starcounter.Internal.MsBuild.Codegen {
    public partial class Gen2DomGenerator {


        private AstJsonClass DefaultJson;

        public AstJsonClass GetDefaultJson() {
            if (DefaultJson == null) {
                DefaultJson = new AstJsonClass(this) {
                    GlobalClassSpecifier = "s::Json<object>",
                    ClassStemIdentifier = "Json"
                };
                DefaultJson.NTemplateClass = new AstSchemaClass(this) {
                    NValueClass = DefaultJson,
                    Template = DefaultObjTemplate,
                    GlobalClassSpecifier = "s::Json<object>.JsonByExample.Schema<s:Json<object>>"
                };
                DefaultJson.NMetadataClass = new AstJsonMetadataClass(this) {
                    NValueClass = DefaultJson,
                    GlobalClassSpecifier = "s::Json<object>.JsonByExample.Metadata<st:Schema<s:Json<object>>,s:Json<object>>"
                };
            }
            return DefaultJson;
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
            //            if (template is Schema<Json<object>>) {
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
                var ns = template.GetType().Namespace;
                string nsa;
                if (ns == "Starcounter")
                    nsa = "s::";
                else
                    nsa = "st::";
                ret.NamespaceAlias = nsa;
                ret.NTemplateClass = ObtainTemplateClass(template);

                ret.NMetadataClass = ObtainMetaClass(template);
                return ret;
            }

            if (template is Schema<Json<object>>) {
                var tjson = template as Schema<Json<object>>;
                var acn = new AstJsonClass(this);
                ValueClasses.Add(template, acn);
                var jsonbyexample = new AstOtherClass(this) {
                    Parent = acn,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true
                };
                acn.NJsonByExample = jsonbyexample;

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
            if (template is ArrSchema<Json<object>>) {
                var tarr = template as ArrSchema<Json<object>>;
                var acn = new AstJsonClass(this);
                ValueClasses.Add(template, acn);
                if (template.Parent != null) {
                    acn.Parent = ObtainValueClass(template.Parent);
                }
                acn.NMetadataClass = ObtainMetaClass(template);
                acn.NTemplateClass = ObtainTemplateClass(template);

                acn.NamespaceAlias = "st::";
                acn.Generic = new AstClass[] {
                        ObtainTemplateClass(tarr.ElementType)
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
                ret.RealType = template.MetadataType;

                ret.NValueClass = ObtainValueClass(template);
                return ret;
            }


            AstInstanceClass parent = null;
            if (template.Parent != null)
                parent = ObtainValueClass(template.Parent);

            if (template is Schema<Json<object>>) {
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
                mcn.Parent = acn.NJsonByExample;
                mcn.ClassStemIdentifier = "Metadata";
//                mcn.GlobalClassSpecifier = HelperFunctions.GetGlobalClassSpecifier(template.MetadataType, true);

//                if (template == DefaultObjTemplate) {
//                    mcn.NamespaceAlias = "st::";
//                }
                return mcn;
            }
            else if (template is ArrSchema<Json<object>>) {
                var tarr = template as ArrSchema<Json<object>>;
                AstClass[] gen;
                gen = new AstClass[] {
                    ObtainTemplateClass(tarr.ElementType),
                    parent
                };
                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };
                MetaClasses.Add(template, mcn);
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
                ret.RealType = type;
 //               ret.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(type);
                ret.NValueClass = ObtainValueClass(template);
                return ret;
            }


            if (template is Schema<Json<object>>) {
                ret = new AstSchemaClass(this) {
                    Template = template
                };
                TemplateClasses.Add(template, ret);
                var acn = (AstJsonClass)ObtainValueClass(template);
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
                ret.Generic = new AstClass[] { acn };
                ret.NValueClass = acn;
                return ret;
            }
            if (template is ArrSchema<Json<object>>) {
                var tarr = template as ArrSchema<Json<object>>;
                ret = new AstTemplateClass(this) {
                    Template = template
                };
                TemplateClasses.Add(template, ret);
                ret.NValueClass = ObtainValueClass(template);
                var acn = ObtainValueClass(tarr.ElementType);
                ret.NamespaceAlias = "st::";
                ret.Generic = new AstClass[] { acn };
                return ret;
            }
            throw new Exception();
        }

    }
}
