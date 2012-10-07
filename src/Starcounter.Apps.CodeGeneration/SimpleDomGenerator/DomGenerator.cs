
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Starcounter.Application.Test")]

namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Simple code-dom generator for the Template class. In a Template tree structure, each Template will be represented by a temporary
    /// CsGen_Template object. The reason for this is to avoid cluttering the original Template code with code generation concerns while
    /// still employing a polymorphic programming model to implement the unique functionality of each type of Template (see the virtual
    /// functions).
    /// </summary>
    /// <remarks>
    /// Class nodes can easily be moved to a new parent by setting the Parent property on the node. This can been done after the DOM tree
    /// has been generated. This is used to allow the generated code structure match the code behind structure. In this way,
    /// there is no need for the programmer to have deep nesting of class declarations in JSON trees.
    /// </remarks>
    public partial class DomGenerator {

        /// <summary>
        /// Creates a dom generator for a template
        /// </summary>
        /// <param name="template">The represented template</param>
        /// <param name="parent">The code generators have a parallel parent/child tree to their represented Templates.
        /// This is the parent C# code generator (corresponding to the Parent of the Template parameter.</param>
        internal DomGenerator(CodeGenerationModule mod, Template template ) { //, string typename, string templateClass, string metadataClass ) {
            Template = template;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a application template.
        /// </summary>
        /// <param name="at">The App template (i.e. json tree prototype) to generate code for</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public NRoot GenerateDomTree( AppTemplate at, CodeBehindMetadata metadata ) {
            var root = new NRoot();
            var acn = new NApp() {
                Parent = root,
                Template = at,
                IsPartial = true
            };
            var tcn = new NAppTemplate() {
                Parent = acn,
                AppNode = acn,
                _Inherits = "AppTemplate"
            };
            var mcn = new NAppMetadata() {
                Parent = acn,
                AppNode = acn,
                _Inherits = "AppMetadata"
            };
            NApp.Instances[at] = acn;
            NAppTemplate.Instances[at] = tcn;
            NAppMetadata.Instances[at] = mcn;


            root.AppClassNode = acn;
            acn.MetaDataClass = mcn;
            acn.TemplateClass = tcn;
            if (acn is NApp) {
                var racn = acn as NApp;
                GenerateKids(racn, racn.TemplateClass, racn.MetaDataClass, racn.Template);
            }

            //tcn.Parent = tcn.Parent;
            //mcn.Parent = mcn.Parent;
            MoveNestedClassToBottom(root);

            //                AppNode = acn,
            var json = new NJsonAttributeClass() {
                Parent = acn,
                IsStatic = true,
                _Inherits = null,
                _ClassName = "Json"
            };

            GenerateJsonAttributes(acn, json);
            var input = new NOtherClass() {
                Parent = acn,
                _ClassName = "Input",
                IsStatic = true
            };
            GeneratePrimitiveValueEvents( input, acn, "Input");

            ConnectCodeBehindClasses(root, metadata);

            return root;
        }

        private void ConnectCodeBehindClasses(NRoot root, CodeBehindMetadata metadata)
        {
            AppTemplate appTemplate;
            AppTemplate rootTemplate;
            AppTemplate[] classesInOrder;
            JsonMapInfo mapInfo;
            NApp nApp;

            classesInOrder = new AppTemplate[metadata.JsonPropertyMapList.Count];
            rootTemplate = root.AppClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++)
            {
                mapInfo = metadata.JsonPropertyMapList[i];

                appTemplate = FindAppTemplateFor(mapInfo.JsonMapName, rootTemplate);
                appTemplate.ClassName = mapInfo.ClassName;
                if (!String.IsNullOrEmpty(mapInfo.Namespace))
                    appTemplate.Namespace = mapInfo.Namespace;

                nApp = NApp.Instances[appTemplate] as NApp;
                nApp.IsPartial = true;
                nApp._Inherits = null;

                classesInOrder[i] = appTemplate;
            }

            ReorderCodebehindClasses(classesInOrder, metadata.JsonPropertyMapList, root);
        }

        private void ReorderCodebehindClasses(AppTemplate[] classesInOrder, 
                                              List<JsonMapInfo> mapInfos, 
                                              NRoot root)
        {
            List<String> parentClasses;
            NBase parent;
            NClass theClass;
            NClass parentClass;
            NOtherClass notExistingClass;
            
            for (Int32 i = 0; i < classesInOrder.Length; i++)
            {
                theClass = NApp.Instances[classesInOrder[i]];
                parentClasses = mapInfos[i].ParentClasses;
                if (parentClasses.Count > 0)
                {
                    parent = root;
                    for (Int32 pi = parentClasses.Count - 1; pi >= 0; pi--)
                    {
                        parentClass = FindClass(parentClasses[pi], parent);
                        if (parentClass != null)
                        {
                            parent = parentClass;
                        }
                        else
                        {
                            notExistingClass = new NOtherClass();
                            notExistingClass._ClassName = parentClasses[pi];
                            notExistingClass.IsPartial = true;
                            notExistingClass.Parent = parent;
                            parent = notExistingClass;
                        }
                    }
                    theClass.Parent = parent;
                }
                else
                {
                    theClass.Parent = root;
                }
            }
        }

        private NClass FindClass(String className, NBase parent)
        {
            NClass nClass;
            for (Int32 i = 0; i < parent.Children.Count; i++)
            {
                nClass = parent.Children[i] as NClass;
                if ((nClass != null) && (nClass.ClassName.Equals(className)))
                {
                    return nClass;
                }
            }
            return null;
        }

        private AppTemplate FindAppTemplateFor(String jsonMapName, AppTemplate rootTemplate)
        {
            AppTemplate appTemplate;
            String[] mapParts;
            Template template;

            appTemplate = rootTemplate;
            mapParts = jsonMapName.Split('.');
            for (Int32 i = 2; i < mapParts.Length; i++)
            {
                template = appTemplate.Properties.GetTemplateByName(mapParts[i]);
                if (template is AppTemplate)
                {
                    appTemplate = (AppTemplate)template;
                }
                else if (template is ListingProperty)
                {
                    appTemplate = ((ListingProperty)template).App;
                }
                else
                {
                    throw new Exception("Invalid property to bind codebehind.");
                }     
            }
            return appTemplate;
        }

        /// <summary>
        /// Provide a nicer default order of the generated classes such
        /// that the primitive properties (string, int, etc.) comes first and
        /// the nested classes comes in the end of the class declaration.
        /// Show the nested App classes before the template and metadata classes.
        /// </summary>
        /// <param name="node">The node containing the children to rearrange</param>
        private void MoveNestedClassToBottom(NBase node)
        {
            var move = new List<NBase>();
            foreach (var kid in node.Children) {
                if (kid is NApp) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is NAppTemplate) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is NAppMetadata) {
                    move.Add(kid);
                }
            }
            foreach (var kid in move) {
                kid.Parent = kid.Parent;
                MoveNestedClassToBottom(kid);
            }            
        }

        private void GenerateKidsForAppTemplate(AppTemplate at, NApp appParent, NClass templParent, NClass metaParent, Template template) {
            NClass acn;
            NClass tcn;
            NClass mcn;
            if (at.Properties.Count == 0) {
                acn = CodeGenerationModule.FixedAppTypes[typeof(AppTemplate)];
                tcn = CodeGenerationModule.FixedTemplateTypes[typeof(AppTemplate)];
                mcn = CodeGenerationModule.FixedMetaDataTypes[typeof(AppTemplate)];
            }
            else {
                NApp racn;
                acn = racn = new NApp() {
                    Parent = appParent,
                    Template = at,
                    _Inherits = "App"
                };
                tcn = new NAppTemplate() {
                    Parent = racn,
                    AppNode = racn,
                    _Inherits = "AppTemplate"
                };
                mcn = new NAppMetadata() {
                    Parent = racn,
                    AppNode = racn,
                    _Inherits = "AppMetadata"
                };
                racn.MetaDataClass = mcn;
                racn.TemplateClass = tcn;
            }
            NApp.Instances[at] = acn;
            NAppTemplate.Instances[at] = tcn;
            NAppMetadata.Instances[at] = mcn;
            if (acn is NApp) {
                GenerateKids(acn as NApp, tcn as NAppTemplate, mcn as NAppMetadata, at);
                if (!appParent.Children.Remove(acn))
                    throw new Exception(); // Move to...
                appParent.Children.Add(acn); // Move to...
                if (!acn.Children.Remove(tcn))
                    throw new Exception(); // Move to...
                acn.Children.Add(tcn); // Move to...
                if (!acn.Children.Remove(mcn))
                    throw new Exception(); // ...last member
                acn.Children.Add(mcn); // ...last member
            }

            if (at.Parent is AppTemplate) {
                new NProperty() {
                    Parent = appParent,
                    Template = at,
                    Type = NApp.Instances[at],
                };
                new NProperty() {
                    Parent = templParent,
                    Template = at,
                    Type = NAppTemplate.Instances[at],
                };
                new NProperty() {
                    Parent = metaParent,
                    Template = at,
                    Type = NAppMetadata.Instances[at],
                };

            }
        }

        private void GenerateKidsForListingProperty(ListingProperty alt, NApp appParent, NClass templParent, NClass metaParent, Template template) {
            var amn = new NProperty() {
                                          Parent = appParent,
                                          Template = alt
                                      };
            var tmn = new NProperty() {
                                          Parent = appParent.TemplateClass,
                                          Template = alt
                                      };
            var mmn = new NProperty() {
                                          Parent = appParent.MetaDataClass,
                                          Template = alt
                                      };
            GenerateKids(appParent, templParent, metaParent, alt);
            amn.Type = new NListingXXXClass("Listing", NApp.Instances[alt.App], null);
            tmn.Type = new NListingXXXClass("ListingProperty", NApp.Instances[alt.App], NAppTemplate.Instances[alt.App]);
            mmn.Type = new NListingXXXClass("ListingMetadata", NApp.Instances[alt.App], NAppTemplate.Instances[alt.App]);
        }

        private void GenerateKids(NApp appParent, NClass templParent, NClass metaParent, Template template) {
            if (template is ParentTemplate) {
                var pt = (ParentTemplate)template;
                foreach (var kid in pt.Children) {
                    if (kid is ParentTemplate) {
                        if (kid is AppTemplate) {
                            GenerateKidsForAppTemplate(kid as AppTemplate, appParent, templParent,metaParent,template);
                        }
                        else if (kid is ListingProperty) {
                            GenerateKidsForListingProperty(kid as ListingProperty, appParent, templParent,metaParent,template);
                        }
                        else {
                            throw new Exception();
                        }
                    }
                    else {
                        new NProperty() {
                            Parent = appParent,
                            Template = kid,
                            Type = CodeGenerationModule.FixedAppTypes[kid.GetType()] //NAppTemplate.Instances[(AppTemplate)kid.Parent]
                        };
                        new NProperty() {
                            Parent = templParent,
                            Template = kid,
                            Type = CodeGenerationModule.FixedTemplateTypes[kid.GetType()] //NAppTemplate.Instances[(AppTemplate)kid.Parent]
                        };
                        new NProperty() {
                            Parent = metaParent,
                            Template = kid,
                            Type = CodeGenerationModule.FixedMetaDataTypes[kid.GetType()]
                        };
                    }
                }
            }
        }

        /// <summary>
        /// The JSON attributes is a set of source code attributes (C# Attributes) used to annotate
        /// which user classes should be used for which JSON tree nodes (objects). This allows the user
        /// to write classes that are not deeply nested (unless he/she wants the class declarations nested).
        /// The function is recursive and calls itself.
        /// </summary>
        /// <param name="app">The node to generate attributes for</param>
        /// <param name="parent">The DOM node to generate attributes for</param>
        public void GenerateJsonAttributes(NApp app,NBase parent) {
            foreach (var kid in app.Children) {
                if (kid is NApp) {
                    var x = new NJsonAttributeClass() {
                        _Inherits = "TemplateAttribute",
                        _ClassName = (kid as NApp).Stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes(kid  as NApp, x);
                }
            }
        }

        /// <summary>
        /// Used to generate Handle( ... ) event classes used by the user programmer to catch events such
        /// as the Input event.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="app"></param>
        /// <param name="eventName">The name of the event (i.e. "Input").</param>
        public void GeneratePrimitiveValueEvents( NBase parent, NClass app, string eventName ) {
            foreach (var kid in app.Children) {
                if (kid is NProperty) {
                    var mn = kid as NProperty;
                    if (mn.Type is NListingXXXClass || mn.Type is NApp) {
                        NClass type;
                        if (mn.Type is NListingXXXClass)
                            type = (mn.Type as NListingXXXClass).NApp;
                        else
                            type = mn.Type as NApp;
                        var x = new NOtherClass() {
                            Parent = parent,
                            IsStatic = true,
                            _ClassName = mn.MemberName
                        };
                        GeneratePrimitiveValueEvents(x, type, eventName);
                    }
                    else {
                        if (mn.Type.IsPrimitive) {
                            var x = new NEventClass() {
                                NMember = mn,
                                Parent = parent,
                                NApp = app,
                                EventName = eventName
                            };
                        }
                    }
                }
            }
        }


        /// <summary>
        /// The field behind the Template property.
        /// </summary>
        internal Template Template;

        /// <summary>
        /// Employed by the template code generator.
        /// </summary>
        internal string GlobalNamespace {
            get {
                Template current = Template;
                while (current.Parent != null) current = (Template)current.Parent;
                return ((AppTemplate)current).Namespace;
            }
        }

    }


}
