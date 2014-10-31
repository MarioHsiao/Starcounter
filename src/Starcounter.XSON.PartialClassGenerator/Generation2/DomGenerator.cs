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

        internal AstRoot Root;
        internal CodeBehindMetadata CodeBehindMetadata;

        internal Dictionary<Template, AstInstanceClass> ValueClasses = new Dictionary<Template, AstInstanceClass>();
        internal Dictionary<Template, AstTemplateClass> TemplateClasses = new Dictionary<Template, AstTemplateClass>();
        internal Dictionary<Type, AstTemplateClass> TemplateClassesByType = new Dictionary<Type, AstTemplateClass>();
        internal Dictionary<Template, AstMetadataClass> MetaClasses = new Dictionary<Template, AstMetadataClass>();

        private TString TPString = new TString();
        private TLong TPLong = new TLong();
        private TDecimal TPDecimal = new TDecimal();
        private TDouble TPDouble = new TDouble();
        private TBool TPBool = new TBool();
        private TTrigger TPAction = new TTrigger();
        private TObject DefaultObjTemplate = null;
        private TArray<Json> DefaultArrayTemplate = null;

        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TObject template, Type defaultNewObjTemplateType, CodeBehindMetadata metadata) {
            DefaultObjTemplate = (TObject)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);

            DefaultObjTemplate.Namespace = "Starcounter";
            DefaultObjTemplate.ClassName = "Json";

            DefaultArrayTemplate = new TArray<Json>();
            DefaultArrayTemplate.ElementType = DefaultObjTemplate; //(TObject)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
//            DefaultArrayTemplate.ElementType.Namespace = "Starcounter";
//            DefaultArrayTemplate.ElementType.ClassName = "Json";

            CodeBehindMetadata = metadata;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The Json template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot GenerateDomTree(TObject at) {
            var p1 = new GeneratorPhase1(this);
            var p2 = new GeneratorPhase2(this);
            var p3 = new GeneratorPhase3(this);
            var p4 = new GeneratorPhase4(this);
            var p5 = new GeneratorPhase5(this);
            var p6 = new GeneratorPhase6(this);

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

        /// <summary>
        /// Finds the class where the Handle method is declared. This can be the same class
        /// as where the property is declared or a parent class.
        /// </summary>
        /// <param name="binding">The node in the AST tree containing the binding info.</param>
        /// <param name="info">The info from the codebehind metadata.</param>
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

        internal AstInstanceClass ObtainDefaultValueClass() {
            return ObtainValueClass(DefaultObjTemplate);
        }

        internal AstInstanceClass ObtainDefaultArrayValueClass() {
            return ObtainValueClass(DefaultArrayTemplate);
        }

        internal void AssociateTemplateWithDefaultJson(TObject template) {
            var defaultJsonClass = ObtainValueClass(DefaultObjTemplate);
            ValueClasses[template] = defaultJsonClass;
            TemplateClasses[template] = defaultJsonClass.NTemplateClass;
            MetaClasses[template] = defaultJsonClass.NMetadataClass;
        }

        internal void AssociateTemplateWithReusedJson(TObject template, string instanceTypeName) {
            CodeBehindClassInfo cci = new CodeBehindClassInfo(null);
            cci.BaseClassName = instanceTypeName;
            var jsonClass = ObtainInheritedValueClass(cci);

            ValueClasses[template] = jsonClass;
            TemplateClasses[template] = jsonClass.NTemplateClass;
            MetaClasses[template] = jsonClass.NMetadataClass;
        }

        /// <summary>
        /// Associates the specified template with astclasses for a default array (I.e Array<Json>)
        /// </summary>
        /// <param name="template"></param>
        internal void AssociateTemplateWithDefaultArray(TObjArr template) {
            var defaultArrayClass = ObtainValueClass(DefaultArrayTemplate);
            ValueClasses[template] = defaultArrayClass;
            TemplateClasses[template] = defaultArrayClass.NTemplateClass;
            MetaClasses[template] = defaultArrayClass.NMetadataClass;
        }

        internal void AssociateTemplateWithReusedArray(TObjArr template, string instanceTypeName) {
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

            ValueClasses[template] = astArray;
            TemplateClasses[template] = astArray.NTemplateClass;
            MetaClasses[template] = astArray.NMetadataClass;

            ValueClasses[template.ElementType] = genericTypeClass;
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
                acn.InheritedClass = ObtainValueClass(DefaultObjTemplate);

                if (template.Parent != null)
                    acn.ParentProperty = ObtainValueClass(template.Parent);
                
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

                if (template.Parent != null) {
                    newJson.Parent = ObtainValueClass(template.Parent);
                }

                acn.Namespace = template.InstanceType.Namespace;
                acn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(template.InstanceType);
                acn.Generic = new AstClass[] { newJson };
                return acn;
            }
            throw new Exception();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapInfo"></param>
        /// <returns></returns>
        internal AstJsonClass ObtainInheritedValueClass(CodeBehindClassInfo mapInfo) {
            AstJsonClass acn;
            if (mapInfo.DerivesDirectlyFromJson) {
                acn = (AstJsonClass)ObtainValueClass(DefaultObjTemplate);
            } else {
                acn = new AstJsonClass(this) {
                    CodebehindClass = new CodeBehindClassInfo(null) {
                        ClassName = mapInfo.BaseClassName,
                        Namespace = null,
                        UseGlobalSpecifier = true
                    },
                    UseClassAlias = true
                };
                var jsonbyexample = new AstOtherClass(this) {
                    Parent = acn,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true,
                    UseClassAlias = true
                };
                var genSchemaClass = new AstSchemaClass(this) {
                    NValueClass = acn,
                    Parent = jsonbyexample,
                    Template = DefaultObjTemplate,
                    IsCodegenerated = true,
                    ClassStemIdentifier = "Schema",
                    UseClassAlias = true
                };
                acn.NJsonByExample = jsonbyexample;
                acn.NTemplateClass = genSchemaClass;
                acn.NMetadataClass = new AstMetadataClass(this) {
                    NValueClass = acn,
                    CodebehindClass = new CodeBehindClassInfo(null) {
                        ClassName = CalculateInnerClassName(mapInfo.BaseClassName, "MEE"),
                    }
                };
            }
            return acn;
        }

        /// <summary>
        /// Returns the given class path for a metadata or template class given
        /// the class path to the Json class (the outer class).
        /// </summary>
        /// <param name="classpath">The classpath for the Json class (i.e.
        /// "somenamespace.someclass"</param>
        /// <param name="prefix">The prefix such as T or M</param>
        /// <returns>The inner class path (i.e. somenamespace.someclass.Tsomeclass)</returns>
        internal string CalculateInnerClassName(string classpath, string prefix) {
            var parts = classpath.Split('.');
            var classname = prefix + parts[parts.Length - 1];
            var str = parts[0];
            for (int t = 1; t < parts.Length; t++) {
                str += "." + parts[t];
            }
            return str + "." + classname;
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
                parent = ObtainValueClass(DefaultObjTemplate);

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

                var inheritedClass = acn.InheritedClass as AstJsonClass;
                if (inheritedClass != null)
                    mcn.InheritedClass = inheritedClass.NMetadataClass;

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

                var inheritedClass = acn.InheritedClass as AstJsonClass;
                if (inheritedClass != null)
                    ret.InheritedClass = inheritedClass.NTemplateClass;

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
    }
}
