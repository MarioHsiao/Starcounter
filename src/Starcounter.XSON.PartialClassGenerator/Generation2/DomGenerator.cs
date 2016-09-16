using System;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
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
        internal AstRoot Root;
        internal CodeBehindMetadata CodeBehindMetadata;

        private Dictionary<Template, AstInstanceClass> valueClasses = new Dictionary<Template, AstInstanceClass>();
        private Dictionary<Template, AstTemplateClass> templateClasses = new Dictionary<Template, AstTemplateClass>();
        private Dictionary<Type, AstTemplateClass> templateClassesByType = new Dictionary<Type, AstTemplateClass>();
        private Dictionary<Template, AstMetadataClass> metaClasses = new Dictionary<Template, AstMetadataClass>();
        private TString protoString = new TString();
        private TLong protoLong = new TLong();
        private TDecimal protoDecimal = new TDecimal();
        private TDouble protoDouble = new TDouble();
        private TBool protoBool = new TBool();
        private TTrigger protoAction = new TTrigger();
        private TValue defaultObjTemplate = null;
        private TArray<Json> defaultArrayTemplate = null;

        private ulong anonymousClassId = 1;

        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TValue template, Type defaultNewTemplateType, CodeBehindMetadata metadata) {
            defaultObjTemplate = new TObject();//(TValue)defaultNewTemplateType.GetConstructor(new Type[0]).Invoke(null);
            defaultObjTemplate.CodegenInfo.Namespace = "Starcounter";
            defaultObjTemplate.CodegenInfo.ClassName = "Json";
            defaultArrayTemplate = new TArray<Json>();
            defaultArrayTemplate.ElementType = defaultObjTemplate;
            defaultArrayTemplate.CodegenInfo.Namespace = "Starcounter";
            CodeBehindMetadata = metadata;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The Json template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot GenerateDomTree(TValue at) {
            var pre = new GeneratorPrePhase(this);
            var p1 = new GeneratorPhase1(this);
            var p2 = new GeneratorPhase2(this);
            var p3 = new GeneratorPhase3(this);
            var p4 = new GeneratorPhase4(this);
            var p5 = new GeneratorPhase5(this);
            var p6 = new GeneratorPhase6(this);
            var p7 = new GeneratorPhase7(this);

            AstJsonClass acn;
            AstSchemaClass tcn;
            AstMetadataClass mcn;

            pre.RunPrePhase(at);

            this.Root = p1.RunPhase1(at, out acn, out tcn, out mcn);
            p2.RunPhase2(acn,tcn,mcn);
            p3.RunPhase3(acn);
            p4.RunPhase4(acn);
            p5.RunPhase5(acn, tcn, mcn);
            p6.RunPhase6(acn);
            p7.RunPhase7(acn);

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
            TValue candidate = binding.PropertyAppClass.Template;
            TValue appTemplate;
            AstJsonClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TValue;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.CodegenInfo.ClassName)) {
                        declaringAppClass = (AstJsonClass)ObtainRootValueClass(appTemplate);
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
        /// Gets the prototype.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>Template.</returns>
        public Template GetPrototype(Template template) {
            if (template is TString) {
                return protoString;
            } else if (template is TLong) {
                return protoLong;
            } else if (template is TDouble) {
                return protoDouble;
            } else if (template is TDecimal) {
                return protoDecimal;
            } else if (template is TBool) {
                return protoBool;
            } else if (template is TTrigger) {
                return protoAction;
            } else if (template is TObject) {
                return defaultObjTemplate;
            } else if (template is TObjArr) {
                return defaultArrayTemplate;
            }
            return template;
        }

        internal void AssociateTemplateWithDefaultJson(TObject template) {
            var defaultJsonClass = ObtainValueClass(defaultObjTemplate);
            valueClasses[template] = defaultJsonClass;
            templateClasses[template] = defaultJsonClass.NTemplateClass;
            metaClasses[template] = defaultJsonClass.NMetadataClass;
        }

        internal void AssociateTemplateWithReusedJson(TObject template, string instanceTypeName) {
            CodeBehindClassInfo cci = new CodeBehindClassInfo(null);
            cci.BaseClassName = instanceTypeName;
            var jsonClass = ObtainInheritedValueClass(cci, template);

            valueClasses[template] = jsonClass;
            templateClasses[template] = jsonClass.NTemplateClass;
            metaClasses[template] = jsonClass.NMetadataClass;
        }

        /// <summary>
        /// Associates the specified template with astclasses for a default array (I.e Array<Json>)
        /// </summary>
        /// <param name="template"></param>
        internal void AssociateTemplateWithDefaultArray(TObjArr template) {
            var arrClass = ObtainValueClass(template);
            var defaultArrClass = ObtainValueClass(defaultArrayTemplate);

            if (arrClass is AstJsonClass) {
                arrClass.InheritedClass = defaultArrClass;
                arrClass.NTemplateClass.InheritedClass = defaultArrClass.NTemplateClass;
            } else {
                valueClasses[template] = defaultArrClass;
                templateClasses[template] = defaultArrClass.NTemplateClass;
                metaClasses[template] = defaultArrClass.NMetadataClass;
            }
        }

        private AstInstanceClass GetDefaultArrayWithDifferentGeneric(Template elementTemplate) {
            var arrValueClass = new AstInstanceClass(this);
            var arrTemplateClass = new AstTemplateClass(this);
            var arrElementClass = ObtainRootValueClass(elementTemplate);

            arrValueClass.Namespace = defaultArrayTemplate.CodegenInfo.Namespace;
            arrValueClass.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(defaultArrayTemplate.InstanceType);
            arrValueClass.NTemplateClass = arrTemplateClass;

            arrTemplateClass.NValueClass = arrValueClass;
            arrTemplateClass.Template = defaultArrayTemplate;

            arrValueClass.Generic = new AstClass[] { arrElementClass };
            arrTemplateClass.Generic = new AstClass[] { arrElementClass };

            return arrValueClass;
        }

        internal void AssociateTemplateWithReusedArray(TObjArr template, string instanceTypeName) {            
            AstJsonClass jsonItemClass;

            var elementTemplate = template.ElementType;
            if (elementTemplate == null) {
                elementTemplate = new TObject();
                elementTemplate.CodegenInfo.Namespace = "Starcounter";
                elementTemplate.CodegenInfo.ClassName = "Json";
                template.ElementType = elementTemplate;
            }

            jsonItemClass = ObtainRootValueClass(elementTemplate);
            jsonItemClass.ClassStemIdentifier = instanceTypeName;

            if (jsonItemClass.CodebehindClass == null)
                jsonItemClass.CodebehindClass = new CodeBehindClassInfo(null);
            jsonItemClass.CodebehindClass.ClassName = instanceTypeName;  

            var valueClass = ObtainValueClass(template);
            valueClass.Generic = new AstClass[] { jsonItemClass };
            valueClass.NTemplateClass.Generic = new AstClass[] { jsonItemClass };

            if (valueClass is AstJsonClass) {
                valueClass.InheritedClass.Generic = new AstClass[] { jsonItemClass };
                valueClass.NTemplateClass.InheritedClass.Generic = new AstClass[] { jsonItemClass };
            }
        }

        /// <summary>
        /// Find the class for the specified template. If the class not yet exists it will
        /// be created as a AstJsonClass for all types of templates (primitve, object, array).
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public AstJsonClass ObtainRootValueClass(Template template) {
            AstJsonClass jsonClass;
            AstInstanceClass cachedClass;
            AstOtherClass jsonByExampleClass;

            if (valueClasses.TryGetValue(template, out cachedClass)) {
                jsonClass = (AstJsonClass)cachedClass;
            } else {
                jsonClass = new AstJsonClass(this);
                valueClasses.Add(template, jsonClass);

                if (template.CodegenInfo.ClassName == null && template.TemplateName == null)
                    template.TemplateName = "Anonymous" + anonymousClassId++;

                if (template is TObjArr) {
                    if (((TObjArr)template).ElementType != null) {
                        jsonClass.InheritedClass = GetDefaultArrayWithDifferentGeneric(((TObjArr)template).ElementType);
                    } else {
                        jsonClass.InheritedClass = GetDefaultArrayWithDifferentGeneric(defaultObjTemplate);
                    }
                } else {
                    jsonClass.InheritedClass = ObtainValueClass(defaultObjTemplate);
                }

                jsonClass.Namespace = template.CodegenInfo.Namespace;
                jsonByExampleClass = new AstOtherClass(this) {
                    Parent = jsonClass,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true
                };
                jsonClass.NJsonByExample = jsonByExampleClass;

                jsonClass.NMetadataClass = ObtainRootMetaClass(template);
                jsonClass.NTemplateClass = ObtainRootSchemaClass(template);
            }
            return jsonClass;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NTemplateClass.</returns>
        public AstTemplateClass ObtainRootSchemaClass(Template template) {
            AstJsonClass jsonClass;
            AstTemplateClass schemaClass;

            if (templateClasses.TryGetValue(template, out schemaClass)) {
                return schemaClass;
            }

            schemaClass = new AstSchemaClass(this) {
                Template = template
            };

            templateClasses.Add(template, schemaClass);

            jsonClass = ObtainRootValueClass(template);
            schemaClass.InheritedClass = ObtainTemplateClass(GetPrototype(template));
            schemaClass.NValueClass = jsonClass;
            schemaClass.Namespace = template.CodegenInfo.Namespace;
            schemaClass.Parent = jsonClass.NJsonByExample;
            schemaClass.ClassStemIdentifier = "Schema";

            if (template is TObjArr){
                Template elementTemplate = ((TObjArr)template).ElementType;
                if (elementTemplate != null) {
                    var tobj = elementTemplate as TObject;
                    if (tobj == null || tobj.Properties.Count > 0)
                        schemaClass.InheritedClass.Generic = new AstClass[] { jsonClass.InheritedClass.Generic[0] };
                }
            }
            
            return schemaClass;
        }

        public AstMetadataClass ObtainRootMetaClass(Template template) {
            AstMetadataClass ret;
            AstInstanceClass parent = null;

            if (metaClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            parent = ObtainValueClass(defaultObjTemplate);

            if (template.IsPrimitive) {
                ret = new AstMetadataClass(this);
                metaClasses.Add(template, ret);
                ret.BuiltInType = template.MetadataType;
                ret.NValueClass = ObtainValueClass(template);
                return ret;
            }

            if (template is TObject) {
                AstClass[] gen;
                gen = new AstClass[] {
                    ObtainTemplateClass(template),
                    parent
                };
                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };

                metaClasses.Add(template, mcn);
                var acn = (AstJsonClass)ObtainValueClass(template);
                mcn.NValueClass = acn;

                var inheritedClass = acn.InheritedClass as AstJsonClass;
                if (inheritedClass != null)
                    mcn.InheritedClass = inheritedClass.NMetadataClass;

                mcn.Namespace = template.CodegenInfo.Namespace;

                // TODO! Add back
                //  mcn.Parent = acn.NJsonByExample;
                mcn.ClassStemIdentifier = "Metadata";
                return mcn;
            } else if (template is TObjArr) {
                var tarr = template as TObjArr;
                AstClass[] gen = null;

                if (tarr.ElementType != null) {
                    gen = new AstClass[] {
                        ObtainTemplateClass(tarr.ElementType),
                        parent
                    };
                } else {
                    gen = new AstClass[] {
                        ObtainTemplateClass(defaultObjTemplate),
                        parent
                    };
                }

                var mcn = new AstJsonMetadataClass(this) {
                    Generic = gen
                };
                metaClasses.Add(template, mcn);

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
                
                metaClasses.Add(template, mcn);
                mcn.NValueClass = ObtainValueClass(template);
                return mcn;
            }
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public AstInstanceClass ObtainValueClass(Template template) {
            AstInstanceClass ret = null;

            if (valueClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                template = GetPrototype(template);
                if (valueClasses.TryGetValue(template, out ret)) {
                    return ret;
                }
            }

            if (template is TObject) {
                var acn = new AstJsonClass(this);
                valueClasses.Add(template, acn);

                acn.InheritedClass = ObtainValueClass(defaultObjTemplate);

                if (template.Parent != null)
                    acn.ParentProperty = ObtainValueClass(template.Parent);

                acn.Namespace = template.CodegenInfo.Namespace;
                var jsonbyexample = new AstOtherClass(this) {
                    Parent = acn,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true
                };
                acn.NJsonByExample = jsonbyexample;
                ret = acn;
            } else if (template.IsPrimitive) {
                ret = new AstPrimitiveType(this);
                valueClasses.Add(template, ret);

                ret.BuiltInType = template.InstanceType;
            } else if (template is TObjArr) {
                var tarr = template as TObjArr;
                var acn = new AstInstanceClass(this);
                valueClasses.Add(template, acn);

                Template elementTemplate = tarr.ElementType;
                TObject elementAsTObj = elementTemplate as TObject;

                bool isUntyped = (elementTemplate == null || (elementAsTObj != null && elementAsTObj.Properties.Count == 0));

                if (isUntyped)
                    elementTemplate = defaultObjTemplate;

                var newJson = ObtainRootValueClass(elementTemplate);

                
                if (!isUntyped && (template.Parent != null))
                    newJson.Parent = ObtainValueClass(template.Parent);
            
                acn.Namespace = template.InstanceType.Namespace;
                acn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(template.InstanceType);
                acn.Generic = new AstClass[] { newJson };
                ret = acn;
            }

            ret.NMetadataClass = ObtainMetaClass(template);
            ret.NTemplateClass = ObtainTemplateClass(template);

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapInfo"></param>
        /// <returns></returns>
        internal AstJsonClass ObtainInheritedValueClass(CodeBehindClassInfo mapInfo, Template template) {
            AstJsonClass acn;
            if (mapInfo.DerivesDirectlyFromJson) {
                acn = null; // (AstJsonClass)ObtainValueClass(defaultObjTemplate);
            } else {
                acn = new AstJsonClass(this) {
                    CodebehindClass = new CodeBehindClassInfo(null) {
                        ClassName = mapInfo.BaseClassName,
                        Namespace = null,
                        UseGlobalSpecifier = false
                    },
                    UseClassAlias = false
                };
                var jsonbyexample = new AstOtherClass(this) {
                    Parent = acn,
                    ClassStemIdentifier = "JsonByExample",
                    IsStatic = true,
                    UseClassAlias = false
                };
                var genSchemaClass = new AstSchemaClass(this) {
                    NValueClass = acn,
                    Parent = jsonbyexample,
                    Template = template,
                    IsCodegenerated = true,
                    ClassStemIdentifier = "Schema",
                    UseClassAlias = false
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
            AstMetadataClass ret;
            AstInstanceClass parent = null;

            if (metaClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                template = GetPrototype(template);
                if (metaClasses.TryGetValue(template, out ret)) {
                    return ret;
                }
            }

            if (template.IsPrimitive) {
                ret = new AstMetadataClass(this);
                ret.BuiltInType = template.MetadataType;
            } else {
                if (template.Parent != null)
                    parent = ObtainValueClass(template.Parent);
                else
                    parent = ObtainValueClass(defaultObjTemplate);

                if (template is TObject) {
                    AstClass[] gen = new AstClass[] { 
                                            ObtainTemplateClass(template), 
                                            parent 
                                     };

                    var mcn = new AstJsonMetadataClass(this) {
                        Generic = gen
                    };
                    
                    var acn = (AstJsonClass)ObtainValueClass(template);
                    mcn.NValueClass = acn;

                    var inheritedClass = acn.InheritedClass as AstJsonClass;
                    if (inheritedClass != null)
                        mcn.InheritedClass = inheritedClass.NMetadataClass;

                    mcn.Namespace = template.CodegenInfo.Namespace;

                    // TODO! Add back
                    //  mcn.Parent = acn.NJsonByExample;
                    mcn.ClassStemIdentifier = "Metadata";
                    ret = mcn;
                } else if (template is TObjArr) {
                    var tarr = template as TObjArr;
                    AstClass[] gen;
                    Template elementTemplate = tarr.ElementType;
                    if (elementTemplate == null)
                        elementTemplate = defaultObjTemplate;

                    gen = new AstClass[] {
                        ObtainRootSchemaClass(elementTemplate),
                        parent
                    };

                    var mcn = new AstJsonMetadataClass(this) {
                        Generic = gen
                    };
                    metaClasses.Add(template, mcn);

                    var tarrType = tarr.GetType();
                    mcn.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(tarrType);
                    mcn.Namespace = tarrType.Namespace;

                    return mcn;
                } else {
                    AstClass[] gen;
                    gen = new AstClass[] {
                    parent
                };
                    var mcn = new AstJsonMetadataClass(this) {
                        Generic = gen
                    };
                    return mcn;
                }
            }

            metaClasses.Add(template, ret);

            ret.NValueClass = ObtainValueClass(template);

            return ret;
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NTemplateClass.</returns>
        public AstTemplateClass ObtainTemplateClass(Template template) {
            AstTemplateClass ret;

            if (templateClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template.IsPrimitive) {
                template = GetPrototype(template);
                if (templateClasses.TryGetValue(template, out ret)) {
                    return ret;
                }
            }

            if (templateClasses.TryGetValue(template, out ret)) {
                return ret;
            }

            if (template is TObject) {
                ret = new AstSchemaClass(this) {
                    Template = template
                };

                templateClasses.Add(template, ret);
                var acn = (AstJsonClass)ObtainValueClass(template);

                var inheritedClass = acn.InheritedClass as AstInstanceClass;
                if (inheritedClass != null)
                    ret.InheritedClass = inheritedClass.NTemplateClass;

                if (template == defaultObjTemplate) {
                    ret.NValueClass = acn;
                    ret.BuiltInType = defaultObjTemplate.GetType();
                } else {
                    ret.Namespace = template.CodegenInfo.Namespace;
                    ret.NValueClass = acn;
                    ret.Parent = acn.NJsonByExample;
                    ret.ClassStemIdentifier = "Schema";
                }
                return ret;
            } else {
                if (template.IsPrimitive) {
                    ret = new AstTemplateClass(this) {
                        Template = template,
                    };

                    templateClasses.Add(template, ret);
                    var type = template.GetType();
                    ret.BuiltInType = type;
                    ret.NValueClass = ObtainValueClass(template);
                } else if (template is TObjArr) {
                    var tarr = template as TObjArr;
                    ret = new AstTemplateClass(this) {
                        Template = template
                    };

                    templateClasses.Add(template, ret);
                    ret.NValueClass = ObtainValueClass(template);
                    var elementTemplate = CheckAndGetDefaultOrArrayElementTemplate(tarr.ElementType);
                    var acn = ObtainRootValueClass(elementTemplate);

                    var tarrType = tarr.GetType();
                    ret.ClassStemIdentifier = HelperFunctions.GetClassStemIdentifier(tarrType);
                    ret.Namespace = tarrType.Namespace;
                    ret.Generic = new AstClass[] { acn };
                } else {
                    throw new Exception();
                }
            }
            return ret;
        }

        /// <summary>
        /// Finds the correct template using the classpath specified in the the code-behind metadata.
        /// </summary>
        /// <param name="rootTemplate">The root template to the search from.</param>
        /// <returns>A template, or null if no match is found.</returns>
        internal TValue FindTemplate(CodeBehindClassInfo ci, TValue rootTemplate) {
            TValue appTemplate;
            string[] mapParts;
            Template template;

            appTemplate = rootTemplate;
            mapParts = ci.ClassPath.Split('.');

            // We skip the two first parts since the first one will always be "json" 
            // and the second the rootTemplate.
            for (Int32 i = 1; i < mapParts.Length; i++) {
                if (!(appTemplate is TObject)) {
                    throw new Exception(
                            String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} is not found.",
                                ci.JsonMapAttribute,
                                mapParts[i]
                            ));
                }

                // We start with i=1. This means that we assume that the first part
                // of the class path is the root class no matter what name is used.
                // This makes it easier when user is refactoring his or her code.

                template = ((TObject)appTemplate).Properties.GetTemplateByPropertyName(mapParts[i]);
                if (template is TObjArr) {
                    appTemplate = ((TObjArr)template).ElementType;
                } else if (template != null) {
                    appTemplate = (TValue)template;
                } else {
                    // TODO:
                    // Change to starcounter errorcode.
                    if (template == null) {
                        throw new Exception(
                            String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} is not found.",
                                ci.JsonMapAttribute,
                                mapParts[i]
                            ));
                    }
                    throw new Exception(
                        String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} has the unsupported type {2}.",
                            ci.JsonMapAttribute,
                            mapParts[i],
                            template.GetType().Name
                        ));
                }
            }
            return appTemplate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="messagePostFix"></param>
        /// <param name="innerException"></param>
        /// <param name="sourceInfo"></param>
        internal void ThrowExceptionWithLineInfo(uint errorCode, 
                                                 string messagePostFix, 
                                                 Exception innerException, 
                                                 ISourceInfo sourceInfo) {
            throw ErrorCode.ToException(
                    errorCode,
                    innerException,
                    messagePostFix,
                    (msg, e) => {
                        return new GeneratorException(msg, sourceInfo);
                    });
        }

        private Template CheckAndGetDefaultOrArrayElementTemplate(Template elementTemplate) {
            if (elementTemplate == null)
                return defaultObjTemplate;

            if (elementTemplate.TemplateTypeId == TemplateTypeEnum.Object) {
                if (((TObject)elementTemplate).Properties.Count == 0)
                    return defaultObjTemplate;
            }
            return elementTemplate;
        }
    }
}
