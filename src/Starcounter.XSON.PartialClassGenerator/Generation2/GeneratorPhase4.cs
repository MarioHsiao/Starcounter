

using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Hooks up the code-behind bind classes and reorders the generated partials
    /// accordingly.
    /// </summary>
    internal class GeneratorPhase4 {

        internal Gen2DomGenerator Generator;

        internal void RunPhase4(AstAppClass acn) {
            ConnectCodeBehindClasses(Generator.Root, Generator.CodeBehindMetadata);
            GenerateInputBindings((AstTAppClass)acn.NTemplateClass, Generator.CodeBehindMetadata);
            // CheckMissingBindingInformation(tcn);
        }




        /// <summary>
        /// Connects the code behind classes.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="metadata">The metadata.</param>
        private void ConnectCodeBehindClasses(AstRoot root, CodeBehindMetadata metadata) {
            TObj appTemplate;
            TObj rootTemplate;
            TObj[] classesInOrder;
            CodeBehindClassInfo mapInfo;
            AstAppClass nAppClass;

            classesInOrder = new TObj[metadata.JsonPropertyMapList.Count];
            rootTemplate = root.AppClassClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                mapInfo = metadata.JsonPropertyMapList[i];

                if (mapInfo.RawJsonMapAttribute != null) {
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

                    nAppClass = Generator.ValueClasses[appTemplate] as AstAppClass;
                    nAppClass.IsPartial = true;
                    nAppClass._Inherits = null;

                    var ntAppClass = Generator.TemplateClasses[appTemplate] as AstTAppClass;
                    var mdAppClass = Generator.MetaClasses[appTemplate] as AstObjMetadata;

                    // if there is codebehind and the class is not inherited from Json we need 
                    // to change the inheritance on the template and metadata classes as well.
                    if (!string.IsNullOrEmpty(mapInfo.BaseClassName) && !mapInfo.BaseClassName.Equals("Json")) {
                        ntAppClass._Inherits = "T" + mapInfo.BaseClassName;
                        mdAppClass._Inherits = mapInfo.BaseClassName + "Metadata";
                    }

                    if (mapInfo.AutoBindToDataObject) {
                        ntAppClass.AutoBindProperties = true;
                    }

                    classesInOrder[i] = appTemplate;
                }
            }

            ReorderCodebehindClasses(classesInOrder, metadata.JsonPropertyMapList, root);
        }

        /// <summary>
        /// Reorders the codebehind classes.
        /// </summary>
        /// <param name="classesInOrder">The classes in order.</param>
        /// <param name="mapInfos">The map infos.</param>
        /// <param name="root">The root.</param>
        private void ReorderCodebehindClasses(TObj[] classesInOrder,
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
                                notExistingClass._ClassName = parentClasses[pi];
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
                if ((nClass != null) && (nClass.ClassName.Equals(className))) {
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
        private TObj FindTAppFor(CodeBehindClassInfo ci, TObj rootTemplate) {
            TObj appTemplate;
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
                if (template is TObj) {
                    appTemplate = (TObj)template;
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
                                ci.RawJsonMapAttribute,
                                mapParts[i]
                            ));
                    }
                    throw new Exception(
                        String.Format("The code-behind tries to bind a class to the json-by-example using the attribute [{0}]. The property {1} has the unsupported type {2}.",
                            ci.RawJsonMapAttribute,
                            mapParts[i],
                            template.GetType().Name
                        ));
                }
            }
            return appTemplate;
        }


        private void CheckMissingBindingInformation(AstTAppClass ntApp) {
            AstArrXXXClass tArr;
            AstTAppClass childTApp;
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

                    tArr = property.Type as AstArrXXXClass;
                    if (tArr != null)
                        childTApp = (AstTAppClass)tArr.NTApp;
                    else
                        childTApp = property.Type as AstTAppClass;

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
                            propertyName = ((TObj)parent).ClassName + propertyName;
                            Generator.ThrowExceptionWithLineInfo(Error.SCERRMISSINGDATATYPEBINDINGJSON, "Path: '" + propertyName + "'", null, property.Template.CompilerOrigin);
                        }
                        CheckMissingBindingInformation(childTApp);
                    }
                }
            }
        }


        /// <summary>
        /// Generates the input bindings.
        /// </summary>
        /// <param name="nApp">The n app.</param>
        /// <param name="metadata">The metadata.</param>
        /// <exception cref="System.Exception">Invalid Handle-method declared in class </exception>
        private void GenerateInputBindings(AstTAppClass nApp,
                                           CodeBehindMetadata metadata) {
            Int32 index;
            List<AstBase> children;
            AstTAppClass propertyAppClass;
            AstConstructor cst;
            AstInputBinding binding;
            AstProperty np;
            Template template;
            string propertyName;
            string[] parts;

            foreach (InputBindingInfo info in metadata.RootClassInfo.InputBindingList) {
                // Find the property the binding is for. 
                // Might not be the same class as the one specified in the info object
                // since the Handle-implementation can be declared in a parent class.
                parts = info.FullInputTypeName.Split('.');
                propertyAppClass = nApp;

                // The first index is always "Input", the next ones until the last is
                // the childapps (the toplevel app is sent as a parameter in here)
                // and the last index is the name of the property.
                for (Int32 i = 1; i < parts.Length - 1; i++) {
                    children = propertyAppClass.Children;
                    np = (AstProperty)children.Find((AstBase child) => {
                        AstProperty property = child as AstProperty;
                        if (property != null)
                            return property.Template.PropertyName.Equals(parts[i]);
                        return false;
                    });

                    template = np.Template;
                    if (template is TObjArr) {
                        template = ((TObjArr)template).ElementType;
                    }
                    propertyAppClass = (AstTAppClass)Generator.FindTemplateClass(template);
                }

                propertyName = parts[parts.Length - 1];

                // We need to search in the children in the constructor since
                // other inputbindings might have been added that makes the
                // insertion index change.
                cst = propertyAppClass.Constructor;
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

                if (index == -1) {
                    // TODO:
                    // Need to add file and linenumbers if possible to pinpoint the erroneous code.
                    // Change to starcounter errorcode.
                    throw new Exception("Invalid Handle-method declared in class "
                                        + info.DeclaringClassName
                                        + ". No property with name "
                                        + propertyName +
                                        " exists.");
                }

                binding = new AstInputBinding(Generator);
                binding.BindsToProperty = (AstProperty)cst.Children[index];
                binding.PropertyAppClass = (AstAppClass)Generator.FindValueClass(propertyAppClass.Template);
                binding.InputTypeName = info.FullInputTypeName;
                Generator.FindHandleDeclaringClass(binding, info);

                // We check the next item in the constructor. All inputbindings for 
                // the same property needs to be ordered with the least parent-calls first.
                Int32 indexToCheck = index + 1;
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

                propertyAppClass.Constructor.Children.Insert(index + 1, binding);
            }
        }


    }
}
