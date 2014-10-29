// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Simple code-dom generator for the Template class. In a Template tree structure,
    /// each Template will be represented by a temporary CsGen_Template object. The reason
    /// for this is to avoid cluttering the original Template code with code generation
    /// concerns while still employing a polymorphic programming model to implement the
    /// unique functionality of each type of Template (see the virtual functions).
    /// </summary>
    /// <remarks>
    /// Class nodes can easily be moved to a new parent by setting the Parent property on
    /// the node. This can been done after the DOM tree has been generated. This is used
    /// to allow the generated code structure match the code behind structure. In this way,
    /// there is no need for the programmer to have deep nesting of class declarations in
    /// JSON trees.
    /// </remarks>
    public class Gen2DomGenerator {
        internal const string InstanceDataTypeName = "InstanceDataTypeName";
        internal const string Reuse = "Reuse";

        private AstOtherClass AstObject;

        private AstJsonClass DefaultJson;
        private AstInstanceClass DefaultJsonArray;

        internal Dictionary<Template, AstInstanceClass> ValueClasses = new Dictionary<Template, AstInstanceClass>();
        internal Dictionary<Template, AstTemplateClass> TemplateClasses = new Dictionary<Template, AstTemplateClass>();
        internal Dictionary<Type, AstTemplateClass> TemplateClassesByType = new Dictionary<Type, AstTemplateClass>();
        internal Dictionary<Template, AstMetadataClass> MetaClasses = new Dictionary<Template, AstMetadataClass>();

        internal TString TPString = new TString();
        internal TLong TPLong = new TLong();
        internal TDecimal TPDecimal = new TDecimal();
        internal TObject DefaultObjTemplate = null;
        internal TDouble TPDouble = new TDouble();
        internal TBool TPBool = new TBool();
        internal TTrigger TPAction = new TTrigger();

        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TObject template, Type defaultNewObjTemplateType, CodeBehindMetadata metadata) {
            DefaultObjTemplate = (TObject)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
            CodeBehindMetadata = metadata;
            AstObject = new AstOtherClass(this) {
                GlobalClassSpecifier = "object",
                NamespaceAlias = null
            };
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The TJson template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot GenerateDomTree(TObject at) {
            var p1 = new GeneratorPhase1(this);
            var p2 = new GeneratorPhase2() { Generator = this };
            var p3 = new GeneratorPhase3() { Generator = this };
            var p4 = new GeneratorPhase4() { Generator = this };
            var p5 = new GeneratorPhase5() { Generator = this };
            var p6 = new GeneratorPhase6() { Generator = this };

            AstJsonClass acn;
            AstSchemaClass tcn;
            AstMetadataClass mcn;

            this.Root = p1.RunPhase1(at, out acn, out tcn, out mcn );
            p2.RunPhase2(acn,tcn,mcn);
            p3.RunPhase3(acn);
            p4.RunPhase4(acn);
            p5.RunPhase5(acn, tcn, mcn);
            p6.RunPhase6(acn);

            return this.Root;
        }

        internal AstRoot Root;
        internal CodeBehindMetadata CodeBehindMetadata;

        private AstBase FindRootNAppClass(AstJsonClass appClassParent) {
            AstBase next = appClassParent;
            while (!(next.Parent is AstRoot))
                next = next.Parent;
            return next;
        }

        /// <summary>
        /// Finds the class where the Handle method is declared. This can be the same class
        /// as where the property is declared or a parentclass.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="info">The info.</param>
        /// <exception cref="System.Exception">Could not find the app where Handle method is declared.</exception>
        internal void FindHandleDeclaringClass(AstInputBinding binding, InputBindingInfo info) {
            Int32 parentCount = 0;
            TContainer candidate = binding.PropertyAppClass.Template;
            TObject appTemplate;
            AstJsonClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TObject;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName)) {
                        declaringAppClass = (AstJsonClass)ObtainValueClass(appTemplate);
                        break;
                    }
                }

                candidate = candidate.Parent;
                parentCount++;
            }

            if (declaringAppClass == null) {
                throw new Exception("Could not find the app where Handle method is declared.");
            }

            binding.DeclaringAppClass = declaringAppClass;
            binding.AppParentCount = parentCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="messagePostFix"></param>
        /// <param name="innerException"></param>
        /// <param name="co"></param>
        internal void ThrowExceptionWithLineInfo(uint errorCode, string messagePostFix, Exception innerException, CompilerOrigin co) {
            var tuple = new Tuple<int, int>(co.LineNo, co.ColNo);
            throw ErrorCode.ToException(
                    errorCode,
                    innerException,
                    messagePostFix,
                    (msg, e) => {
                        return Starcounter.Internal.JsonTemplate.Error.CompileError.Raise<Exception>(msg, tuple, co.FileName);
                    });
        }


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

        public AstJsonClass GetJsonClass(string instanceTypeName, Template template) {
            string schemaName = instanceTypeName + ".JsonByExample.Schema";
            string metadataName = instanceTypeName + ".JsonByExample.Metadata<" + schemaName + "," + instanceTypeName + ">";

            var astJson = new AstJsonClass(this) {
                GlobalClassSpecifier = instanceTypeName,
                ClassStemIdentifier = instanceTypeName
            };
            astJson.NTemplateClass = new AstSchemaClass(this) {
                NValueClass = astJson,
                Template = template,
                GlobalClassSpecifier = schemaName,
                ClassStemIdentifier = "Schema"
            };
            astJson.NMetadataClass = new AstJsonMetadataClass(this) {
                NValueClass = astJson,
                GlobalClassSpecifier = metadataName,
                ClassStemIdentifier = "Metadata"
            };
            return astJson;
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
            var genericTypeClass = new AstJsonClass(this) { ClassStemIdentifier = instanceTypeName, ParentProperty = astArray };
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
        /// Gets the prototype.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Template.</returns>
        public Template GetPrototype(Template template) {
            if (template is TString) {
                return TPString;
            } else if (template is TLong) {
                return TPLong;
            } else if (template is TDouble) {
                return TPDouble;
            } else if (template is TDecimal) {
                return TPDecimal;
            } else if (template is TBool) {
                return TPBool;
            } else if (template is TTrigger) {
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

                acn.NMetadataClass = ObtainMetaClass(template);
                acn.NTemplateClass = ObtainTemplateClass(template);
                return acn;
            } else if (template is TObjArr) {
                var tarr = template as TObjArr;
                var acn = new AstInstanceClass(this);
                ValueClasses.Add(template, acn);
                acn.NMetadataClass = ObtainMetaClass(template);
                acn.NTemplateClass = ObtainTemplateClass(template);

                var newJson = ObtainValueClass(tarr.ElementType);
                newJson.Parent = ObtainValueClass(template.Parent);

                acn.Namespace = template.InstanceType.Namespace;
                acn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(template.InstanceType);
                acn.Generic = new AstClass[] { newJson };
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
                return mcn;
            } else if (template is TObjArr) {
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
            } else {
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
            AstTemplateClass ret;

            if (template.IsPrimitive) {
                template = GetPrototype(template);
            }

            if (TemplateClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                ret = new AstTemplateClass(this) {
                    Template = template,
                };
                TemplateClasses.Add(template, ret);
                var type = template.GetType();
                ret.BuiltInType = type;
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
                ret.Namespace = tarrType.Namespace;
                ret.Generic = new AstClass[] { acn };
                return ret;
            }
            throw new Exception();
        }



    }
}
