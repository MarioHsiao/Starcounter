﻿

using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using TJson = Starcounter.Templates.Schema<Starcounter.Json<object>>;


namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Hooks up the code-behind bind classes and reorders the generated partials
    /// accordingly.
    /// </summary>
    internal class GeneratorPhase4 {

        internal Gen2DomGenerator Generator;

        internal void RunPhase4(AstJsonClass acn) {
            ConnectCodeBehindClasses(Generator.Root, Generator.CodeBehindMetadata);
            GenerateInputBindings(Generator.Root);
            // CheckMissingBindingInformation(tcn);
        }

        private void GenerateInputBindings(AstBase node) {
            if (node is AstSchemaClass) {
                GenerateInputBindingsForASingleClass((AstSchemaClass)node);
            }
            foreach (var kid in node.Children) {
                GenerateInputBindings(kid);
            }
        }




        /// <summary>
        /// Connects the code behind classes.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="metadata">The metadata.</param>
        private void ConnectCodeBehindClasses(AstRoot root, CodeBehindMetadata metadata) {
			int dotIndex;
			string templateClassName;
			string metadataClassName;
            TJson appTemplate;
            TJson rootTemplate;
            TJson[] classesInOrder;
            CodeBehindClassInfo mapInfo;
            AstJsonClass nAppClass;

            classesInOrder = new TJson[metadata.JsonPropertyMapList.Count];
            rootTemplate = (TJson)root.AppClassClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                mapInfo = metadata.JsonPropertyMapList[i];

                if (mapInfo.IsMapped) {
                    appTemplate = FindTAppFor(mapInfo, rootTemplate);

                    if (appTemplate.InstanceDataTypeName != null) {
                        Generator.ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, appTemplate.CompilerOrigin);
                    }

                    // TODO:
                    // If we have an empty object declaration in the jsonfile and
                    // set an codebehind class on that property the TApp here 
                    // will be a generic empty one, which (I guess?) cannot be updated
                    // here.
                    // Do we need to create a new TApp and replace the existing one
                    // for all NClasses?
                    appTemplate.ClassName = mapInfo.ClassName;
                    if (!String.IsNullOrEmpty(mapInfo.Namespace))
                        appTemplate.Namespace = mapInfo.Namespace;

                    nAppClass = (AstJsonClass)Generator.ValueClasses[appTemplate] as AstJsonClass;
                    nAppClass.IsPartial = true;
                    //nAppClass._Inherits = null;

                    var ntAppClass = Generator.TemplateClasses[appTemplate] as AstSchemaClass;
                    var mdAppClass = Generator.MetaClasses[appTemplate];

                    nAppClass.MatchedClass = mapInfo;

                    var outsider = ObtainInheritedJsonClass(mapInfo);
                    nAppClass.InheritedClass = outsider;
                    ntAppClass.InheritedClass = outsider.NTemplateClass;
                     mdAppClass.InheritedClass = outsider.NMetadataClass;

                    // if there is codebehind and the class is not inherited from Json we need 
                    // to change the inheritance on the template and metadata classes as well.
//                    if (!string.IsNullOrEmpty(mapInfo.BaseClassName) && !mapInfo.BaseClassName.Equals("Json")) {
                    if (!mapInfo.DerivesDirectlyFromJson) {

						dotIndex = mapInfo.BaseClassName.LastIndexOf('.');
						templateClassName = mapInfo.BaseClassName + ".";
						metadataClassName = mapInfo.BaseClassName + ".";

						if (dotIndex == -1) {
							templateClassName += "T" + mapInfo.BaseClassName;
							metadataClassName += mapInfo.BaseClassName + "Metadata";
						} else {
							dotIndex++;
							templateClassName += "T" + mapInfo.BaseClassName.Substring(dotIndex);
							metadataClassName += mapInfo.BaseClassName.Substring(dotIndex) + "Metadata";
						}

//						ntAppClass._Inherits = templateClassName;
//						mdAppClass._Inherits = metadataClassName;
                    }

//                    if (mapInfo.AutoBindToDataObject) {
//                        ntAppClass.AutoBindProperties = true;
//                   }

                    classesInOrder[i] = appTemplate;
                }
            }

            ReorderCodebehindClasses(classesInOrder, metadata.JsonPropertyMapList, root);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapInfo"></param>
        /// <returns></returns>
        private AstJsonClass ObtainInheritedJsonClass(CodeBehindClassInfo mapInfo) {
            AstJsonClass acn;
            if (mapInfo.DerivesDirectlyFromJson) {

                acn = Generator.GetDefaultJson();
                //                acn = (AstJsonClass)Generator.ValueClasses[Generator.DefaultObjTemplate];
            }
            else {
                acn = new AstJsonClass(Generator) {
                    MatchedClass = new CodeBehindClassInfo(null) {
                        ClassName = mapInfo.BaseClassName,
                        GenericArg = mapInfo.BaseClassGenericArg
                    },
                };
                acn.NTemplateClass = new AstSchemaClass(Generator) {
                    NValueClass = acn,
                    Template = Generator.DefaultObjTemplate,
                    IsCodegenerated = true
                };
//                acn.Generic = new AstOtherClass(Generator) {
//                    _ClassName = "Schema",
//                    NamespaceAlias = "st::",
//                    Generics = "__Tjsonobj__"
//                };
                acn.NMetadataClass = new AstMetadataClass(Generator) {
                    NValueClass = acn,
                    MatchedClass = new CodeBehindClassInfo(null) {
                        ClassName = CalculateInnerClassName(mapInfo.BaseClassName, mapInfo.BaseClassGenericArg, "MEE"),
                        GenericArg = null //mapInfo.BaseClassGenericArg,
                    }
//                    Template = Generator.DefaultObjTemplate
                };
            }
            return acn;
        }

        /// <summary>
        /// Reorders the codebehind classes.
        /// </summary>
        /// <param name="classesInOrder">The classes in order.</param>
        /// <param name="mapInfos">The map infos.</param>
        /// <param name="root">The root.</param>
        private void ReorderCodebehindClasses(TJson[] classesInOrder,
                                              List<CodeBehindClassInfo> mapInfos,
                                              AstRoot root) {
            List<string> parentClasses;
            AstBase parent;
            AstClass theClass;
            AstClass parentClass;
            AstOtherClass notExistingClass;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                var cls = classesInOrder[i];
                if (cls != null) {
                    theClass = Generator.ValueClasses[cls];
                    parentClasses = mapInfos[i].ParentClasses;
                    if (parentClasses.Count > 0) {
                        parent = root;
                        for (Int32 pi = parentClasses.Count - 1; pi >= 0; pi--) {
                            parentClass = FindClass(parentClasses[pi], parent);
                            if (parentClass != null) {
                                parent = parentClass;
                            }
                            else {
                                notExistingClass = new AstOtherClass(Generator);
                                notExistingClass.ClassStemIdentifier = parentClasses[pi];
                                notExistingClass.IsPartial = true;
                                notExistingClass.Parent = parent;
                                parent = notExistingClass;
                            }
                        }
                        theClass.Parent = parent;
                    }
                    else {
                        theClass.Parent = root;
                    }
                }
            }
        }

        /// <summary>
        /// Finds the class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>NClass.</returns>
        private AstClass FindClass(string className, AstBase parent) {
            AstClass nClass;
            for (Int32 i = 0; i < parent.Children.Count; i++) {
                nClass = parent.Children[i] as AstClass;
                if ((nClass != null) && (nClass.ClassStemIdentifier.Equals(className))) {
                    return nClass;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the app template for.
        /// </summary>
        /// <param name="jsonMapName">Name of the json map.</param>
        /// <param name="rootTemplate">The root template.</param>
        /// <returns>TApp.</returns>
        /// <exception cref="System.Exception">Invalid property to bind codebehind.</exception>
        private TJson FindTAppFor(CodeBehindClassInfo ci, TJson rootTemplate) {
            TJson appTemplate;
            string[] mapParts;
            Template template;

#if DEBUG
            if (ci.ClassPath.Contains(".json."))
                throw new Exception("The class path should be free from .json. text");
#endif
            appTemplate = rootTemplate;


            mapParts = ci.ClassPath.Split('.');

            // We skip the two first parts since the first one will always be "json" 
            // and the second the rootTemplate.
            for (Int32 i = 1; i < mapParts.Length; i++) {
                // We start with i=1. This means that we assume that the first part
                // of the class path is the root class no matter what name is used.
                // This makes it easier when user is refactoring his or her code.
                template = appTemplate.Properties.GetTemplateByPropertyName(mapParts[i]);
                if (template is TJson) {
                    appTemplate = (TJson)template;
                }
                else if (template is TObjArr) {
                    appTemplate = ((TObjArr)template).ElementType;
                }
                else {
                    // TODO:
                    // Change to starcounter errorcode.
                    if (template == null) {
                        throw new Exception(
                            String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} is not found.",
                                ci.RawDebugJsonMapAttribute,
                                mapParts[i]
                            ));
                    }
                    throw new Exception(
                        String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} has the unsupported type {2}.",
                            ci.RawDebugJsonMapAttribute,
                            mapParts[i],
                            template.GetType().Name
                        ));
                }
            }
            return appTemplate;
        }


        private void CheckMissingBindingInformation(AstSchemaClass ntApp) {
//            AstArrXXXClass tArr;
            AstSchemaClass childTApp;
            AstProperty property;
            string propertyName;

            if (!ntApp.AutoBindProperties)
                return;

            foreach (AstBase nb in ntApp.Children) {
                property = nb as AstProperty;
                if (property != null) {
                    propertyName = property.Template.PropertyName;
                    if (string.IsNullOrEmpty(propertyName) || propertyName[0] == '_')
                        continue;

//                    tArr = property.Type as AstArrXXXClass;
//                    if (tArr != null)
//                        childTApp = (AstTAppClass)tArr.NTApp;
//                    else
                        childTApp = property.Type as AstSchemaClass;

                    if (childTApp != null) {
                        if (!childTApp.AutoBindProperties) {
                            // We have a property which is an array or an object that should be bound but 
                            // AutoBindProperties is false which means that we have no type information.

                            // Get the full path for the current property (including classname)
                            Template parent = property.Template.Parent;
                            while (true) {
                                propertyName = parent.PropertyName + "." + propertyName;
                                if (parent.Parent == null)
                                    break;
                                parent = parent.Parent;
                            }
                            propertyName = ((TJson)parent).ClassName + propertyName;
                            Generator.ThrowExceptionWithLineInfo(Error.SCERRMISSINGDATATYPEBINDINGJSON, "Path: '" + propertyName + "'", null, property.Template.CompilerOrigin);
                        }
                        CheckMissingBindingInformation(childTApp);
                    }
                }
            }
        }

        private void FindInputProperty(ref AstSchemaClass tcn, InputBindingInfo info, out AstConstructor cst, out int index ) {
            List<AstBase> children;
            AstProperty np;
            Template template;

			// Find the class where the handle is declared.
			string classname = info.DeclaringClassName;
			AstClass astClass = tcn;
			while (astClass != null) {
				if (classname.Equals(astClass.ClassStemIdentifier))
					break;
				astClass = astClass.Parent as AstClass;
			}

			if (astClass == null) {
				throw new Exception("Cannot find the class '"
										+ info.DeclaringClassName
										+ "' where the Handle method for "
										+ info.FullInputTypeName.Replace("Input.", "") +
										" is implemented.");
			}

            var parts = info.FullInputTypeName.Split('.');

            // The first index is always "Input", the next ones until the last is
            // the childapps (the toplevel app is sent as a parameter in here)
            // and the last index is the name of the property.
			if (parts.Length > 2) {
				for (Int32 i = 1; i < parts.Length - 1; i++) {
					children = astClass.Children;
					np = (AstProperty)children.Find((AstBase child) => {
						AstProperty property = child as AstProperty;
						if (property != null)
							return property.Template.PropertyName.Equals(parts[i]);
						return false;
					});

					if (np == null) {
						throw new Exception("Invalid Handle-method declared in class "
											+ info.DeclaringClassName
											+ ". Cannot find '"
											+ parts[i] 
											+ "' in path " 
											+ info.FullInputTypeName 
											+ ".");
					}

					template = np.Template;
					if (template is TObjArr) {
						template = ((TObjArr)template).ElementType;
					}
					tcn = (AstSchemaClass)Generator.ObtainTemplateClass(template);
				}
			} else {
				tcn = (AstSchemaClass)((AstJsonClass)astClass).NTemplateClass;
			}

            var propertyName = parts[parts.Length - 1];

            // We need to search in the children in the constructor since
            // other inputbindings might have been added that makes the
            // insertion index change.
            cst = tcn.Constructor;
            children = cst.Children;

            // TODO: 
            // Make sure that the inputbindings are added in the correct order if 
            // we have more than one handler for the same property.
            index = children.FindIndex((AstBase child) => {
                AstProperty property = child as AstProperty;
                if (property != null)
                    return property.MemberName.Equals(propertyName);
                return false;
            });

        }

        /// <summary>
        /// Generates the input bindings.
        /// </summary>
        /// <param name="nApp">The n app.</param>
        /// <param name="metadata">The metadata.</param>
        /// <exception cref="System.Exception">Invalid Handle-method declared in class </exception>
        private void GenerateInputBindingsForASingleClass(AstSchemaClass templateClass) {
           // Int32 index;
            AstSchemaClass tcn;
            AstConstructor cst;
            AstInputBinding binding;
            int index;
            //var metadata = Generator.CodeBehindMetadata;
            //string propertyName;
            //string[] parts;

            var classInfo = ((AstJsonClass)templateClass.NValueClass).MatchedClass;

            if (classInfo == null) {
                return;
            }

            foreach (InputBindingInfo info in classInfo.InputBindingList) {
				

                // Find the property the binding is for. 
                // Might not be the same class as the one specified in the info object
                // since the Handle-implementation can be declared in a parent class.
				tcn = templateClass;
                FindInputProperty(ref tcn, info, out cst, out index);

                if (index == -1) {
                    // TODO:
                    // Need to add file and linenumbers if possible to pinpoint the erroneous code.
                    // Change to starcounter errorcode.
                    throw new Exception("Invalid Handle-method declared in class "
                                        + info.DeclaringClassName
                                        + ". No property with name "
										+ info.FullInputTypeName.Replace("Input.", "") +
                                        " exists.");
                }

				binding = new AstInputBinding(Generator);
                binding.BindsToProperty = (AstProperty)cst.Children[index];
                binding.PropertyAppClass = (AstJsonClass)Generator.ObtainValueClass(tcn.Template);
                binding.InputTypeName = info.FullInputTypeName;
				Generator.FindHandleDeclaringClass(binding, info);
                
                // We check the next item in the constructor. All inputbindings for 
                // the same property needs to be ordered with the least parent-calls first.
                Int32 indexToCheck = index + 1;
                var children = cst.Children;

                while (indexToCheck < children.Count) {
                    AstInputBinding otherBinding = children[indexToCheck] as AstInputBinding;

                    if (otherBinding == null)
                        break;
                    if (otherBinding.BindsToProperty != binding.BindsToProperty)
                        break;

                    // Two handlers (or more) are declared for the same property. Lets
                    // order them with the least parentcalls first.
                    if (binding.AppParentCount < otherBinding.AppParentCount)
                        break;

                    index = indexToCheck;
                    indexToCheck++;
                }

                tcn.Constructor.Children.Insert(index + 1, binding);
            }
        }


        /// <summary>
        /// Returns the given class path for a metadata or template class given
        /// the class path to the Json class (the outer class).
        /// </summary>
        /// <param name="classpath">The classpath for the Json class (i.e.
        /// "somenamespace.someclass"</param>
        /// <param name="prefix">The prefix such as T or M</param>
        /// <returns>The inner class path (i.e. somenamespace.someclass.Tsomeclass)</returns>
        internal string CalculateInnerClassName(string classpath, string generics, string prefix) {
            var parts = classpath.Split('.');
            var classname = prefix + parts[parts.Length - 1];
            if (generics != null) {
                parts[parts.Length-1] = parts[parts.Length-1] + "<" + generics + ">";
            }
            var str = parts[0];
            for (int t = 1; t < parts.Length; t++) {
                str += "." + parts[t];
            }
            return str + "." + classname;
        }
    }
}
