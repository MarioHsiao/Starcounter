using System;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Hooks up the code-behind bind classes and reorders the generated partials accordingly.
    /// </summary>
    internal class GeneratorPhase4 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase4(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase4(AstJsonClass acn) {
            ConnectCodeBehindClasses(generator.Root, generator.CodeBehindMetadata);
            GenerateInputBindings(generator.Root);
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
        /// Connects the classes in code-behind to the corresponding template.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="metadata">The metadata.</param>
        private void ConnectCodeBehindClasses(AstRoot root, CodeBehindMetadata metadata) {
            TValue appTemplate;
            TValue rootTemplate;
            TValue[] classesInOrder;
            CodeBehindClassInfo mapInfo;
            AstJsonClass nAppClass;

            classesInOrder = new TValue[metadata.CodeBehindClasses.Count];
            rootTemplate = root.AppClassClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                mapInfo = metadata.CodeBehindClasses[i];

                if (mapInfo.IsMapped) {
                    appTemplate = FindTemplate(mapInfo, rootTemplate);

                    if (appTemplate.CodegenInfo.BoundToType != null) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, appTemplate.CodegenInfo.SourceInfo);
                    }

                    // TODO:
                    // If we have an empty object declaration in the jsonfile and
                    // set an codebehind class on that property the TApp here 
                    // will be a generic empty one, which (I guess?) cannot be updated
                    // here.
                    // Do we need to create a new TApp and replace the existing one
                    // for all NClasses?
                    appTemplate.CodegenInfo.ClassName = mapInfo.ClassName;
                    if (!String.IsNullOrEmpty(mapInfo.Namespace))
                        appTemplate.CodegenInfo.Namespace = mapInfo.Namespace;

                    nAppClass = (AstJsonClass)generator.ObtainValueClass(appTemplate);
                    nAppClass.IsPartial = true;

                    var ntAppClass = (AstSchemaClass)generator.ObtainTemplateClass(appTemplate);
                    var mdAppClass = generator.ObtainMetaClass(appTemplate);

                    nAppClass.CodebehindClass = mapInfo;

                    var outsider = generator.ObtainInheritedValueClass(mapInfo, appTemplate);
                    if (outsider != null) {
                        nAppClass.InheritedClass = outsider;
                        ntAppClass.InheritedClass = outsider.NTemplateClass;
                        mdAppClass.InheritedClass = outsider.NMetadataClass;
                    }

                    //                    if (mapInfo.AutoBindToDataObject) {
                    //                        ntAppClass.AutoBindProperties = true;
                    //                   }

                    classesInOrder[i] = appTemplate;
                }
            }

            ReorderCodebehindClasses(classesInOrder, metadata.CodeBehindClasses, root);
        }

        /// <summary>
        /// Generates astnodes for inputbindings, i.e. connecting methods in code-behind for
        /// validating input from clients.
        /// </summary>
        /// <param name="schemaClass">The class to generate inputbindings for.</param>
        private void GenerateInputBindingsForASingleClass(AstSchemaClass schemaClass) {
            AstProperty theProperty;
            AstSchemaClass declaringSchemaClass;
            AstInputBinding binding;
            int index;

            var classInfo = ((AstJsonClass)schemaClass.NValueClass).CodebehindClass;

            if (classInfo == null) {
                return;
            }

            foreach (InputBindingInfo info in classInfo.InputBindingList) {
                // Find the property the binding is for. 
                // The schemaclass that declares the property might not be the same 
                // as the one specified in the info object since the Handle-implementation 
                // can be declared in a parent class.
                theProperty = FindPropertyForInput(schemaClass, info, out index);
                declaringSchemaClass = (AstSchemaClass)theProperty.Parent.Parent;

                binding = new AstInputBinding(generator);
                binding.BindsToProperty = theProperty;
                binding.PropertyAppClass = (AstJsonClass)generator.ObtainValueClass(declaringSchemaClass.Template);
                binding.InputTypeName = info.FullInputTypeName;
                generator.FindHandleDeclaringClass(binding, info);

                // We check the next item in the constructor. All inputbindings for 
                // the same property needs to be ordered with the least parent-calls first.
                Int32 indexToCheck = index + 1;
                var children = declaringSchemaClass.Constructor.Children;

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

                declaringSchemaClass.Constructor.Children.Insert(index + 1, binding);
            }
        }

        /// <summary>
        /// Reorders the codebehind classes.
        /// </summary>
        /// <param name="classesInOrder">The classes in order.</param>
        /// <param name="mapInfos">The map infos.</param>
        /// <param name="root">The root.</param>
        private void ReorderCodebehindClasses(TValue[] classesInOrder,
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
                    theClass = generator.ObtainValueClass(cls);
                    parentClasses = mapInfos[i].ParentClasses;
                    if (parentClasses.Count > 0) {
                        parent = root;
                        for (Int32 pi = parentClasses.Count - 1; pi >= 0; pi--) {
                            parentClass = FindChildClass(parentClasses[pi], parent);
                            if (parentClass != null) {
                                parent = parentClass;
                            } else {
                                notExistingClass = new AstOtherClass(generator);
                                notExistingClass.ClassStemIdentifier = parentClasses[pi];
                                notExistingClass.IsPartial = true;
                                notExistingClass.Parent = parent;
                                parent = notExistingClass;
                            }
                        }
                        theClass.Parent = parent;
                    } else {
                        theClass.Parent = root;
                    }
                }
            }
        }

        /// <summary>
        /// Searches in the parents children for an AstClass that have the 
        /// same name as the specified classname.
        /// </summary>
        /// <param name="className">Name of the class to find.</param>
        /// <param name="parent">The parent to look in.</param>
        /// <returns>An AstClass, or null if no match was found.</returns>
        private AstClass FindChildClass(string className, AstBase parent) {
            return (AstClass)parent.Children.Find((child) => {
                var astClass = child as AstClass;
                if ((astClass != null) && astClass.ClassStemIdentifier.Equals(className))
                    return true;
                return false;
            });
        }

        /// <summary>
        /// Finds the correct template using the classpath specified in the the code-behind metadata.
        /// </summary>
        /// <param name="rootTemplate">The root template to the search from.</param>
        /// <returns>A template, or null if no match is found.</returns>
        private TValue FindTemplate(CodeBehindClassInfo ci, TValue rootTemplate) {
            TValue appTemplate;
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
        /// Finds the property that corresponds to the information specified in the code-behind.
        /// </summary>
        /// <param name="parentSchemaClass">The starting schema.</param>
        /// <param name="info">Info from code-behind.</param>
        /// <param name="index">The index in the constructor of the declaring schema.</param>
        /// <returns>The property.</returns>
        private AstProperty FindPropertyForInput(AstSchemaClass parentSchemaClass, InputBindingInfo info, out int index) {
            List<AstBase> children;
            AstProperty np;
            Template template;

            // Find the class where the handle is declared.
            string classname = info.DeclaringClassName;
            AstClass astClass = parentSchemaClass;
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
                    parentSchemaClass = (AstSchemaClass)generator.ObtainTemplateClass(template);
                }
            } else {
                parentSchemaClass = (AstSchemaClass)((AstJsonClass)astClass).NTemplateClass;
            }

            var propertyName = parts[parts.Length - 1];

            // We need to search in the children in the constructor since
            // other inputbindings might have been added that makes the
            // insertion index change.
            children = parentSchemaClass.Constructor.Children;


            // TODO: 
            // Make sure that the inputbindings are added in the correct order if 
            // we have more than one handler for the same property.
            index = children.FindIndex((AstBase child) => {
                AstProperty property = child as AstProperty;
                if (property != null)
                    return property.MemberName.Equals(propertyName);
                return false;
            });

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
            return (AstProperty)parentSchemaClass.Constructor.Children[index];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ntApp"></param>
        private void CheckMissingBindingInformation(AstSchemaClass ntApp) {
            AstSchemaClass childTApp;
            AstProperty property;
            string propertyName;

            if (!(ntApp.BindChildren == BindingStrategy.Bound))
                return;

            foreach (AstBase nb in ntApp.Children) {
                property = nb as AstProperty;
                if (property != null) {
                    propertyName = property.Template.PropertyName;
                    if (string.IsNullOrEmpty(propertyName) || propertyName[0] == '_')
                        continue;

                    childTApp = property.Type as AstSchemaClass;

                    if (childTApp != null) {
                        if (!(childTApp.BindChildren == BindingStrategy.Bound)) {
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
                            propertyName = ((TObject)parent).CodegenInfo.ClassName + propertyName;
                            generator.ThrowExceptionWithLineInfo(Error.SCERRMISSINGDATATYPEBINDINGJSON, "Path: '" + propertyName + "'", null, property.Template.CodegenInfo.SourceInfo);
                        }
                        CheckMissingBindingInformation(childTApp);
                    }
                }
            }
        }
    }
}
