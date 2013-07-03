// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// Simple code-dom generator for the Template class. In a Template tree structure,
    /// each Template will be represented by a temporary CsGen_Template object. The reason
    /// for this is to avoid cluttering the original Template code with code generation
    /// concerns while still employing a polymorphic programming model to implement the
    /// unique functionality of each type of Template (see the virtual functions).
    /// </summary>
    /// <remarks>Class nodes can easily be moved to a new parent by setting the Parent property on
    /// the node. This can been done after the DOM tree has been generated. This is used
    /// to allow the generated code structure match the code behind structure. In this way,
    /// there is no need for the programmer to have deep nesting of class declarations in
    /// JSON trees.</remarks>
    public class DomGenerator {
        internal DomGenerator(CodeGenerationModule mod, TObj template, Type defaultNewObjTemplateType) {
            DefaultObjTemplate = (TObj)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
            InitTemplateClasses();
            InitMetadataClasses();
            InitValueClasses();
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, NValueClass> ValueClasses = new Dictionary<Template, NValueClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, NTemplateClass> TemplateClasses = new Dictionary<Template, NTemplateClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, NMetadataClass> MetaClasses = new Dictionary<Template, NMetadataClass>();

        /// <summary>
        /// Initializes static members of the <see cref="NTemplateClass" /> class.
        /// </summary>
        void InitTemplateClasses() {
            TemplateClasses[TPString] = new NPropertyClass(this) { Template = TPString };
            TemplateClasses[TPLong] = new NPropertyClass(this) { Template = TPLong };
            TemplateClasses[TPDecimal] = new NPropertyClass(this) { Template = TPDecimal };
            TemplateClasses[TPDouble] = new NPropertyClass(this) { Template = TPDouble };
            TemplateClasses[TPBool] = new NPropertyClass(this) { Template = TPBool };
            TemplateClasses[TPAction] = new NPropertyClass(this) { Template = TPAction };
            TemplateClasses[DefaultObjTemplate] = new NTAppClass(this) { Template = DefaultObjTemplate };
        }

        /// <summary>
        /// Initializes static members of the <see cref="NMetadataClass" /> class.
        /// </summary>
        void InitMetadataClasses() {
            MetaClasses[TPString] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPString] };
            MetaClasses[TPLong] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPLong] };
            MetaClasses[TPDecimal] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            MetaClasses[TPDouble] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPDouble] };
            MetaClasses[TPBool] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPBool] };
            MetaClasses[TPAction] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[TPAction] };
            MetaClasses[DefaultObjTemplate] = new NMetadataClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Initializes static members of the <see cref="NValueClass" /> class.
        /// </summary>
        void InitValueClasses() {
            ValueClasses[TPString] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPString] };
            ValueClasses[TPLong] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPLong] };
            ValueClasses[TPDecimal] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            ValueClasses[TPDouble] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDouble] };
            ValueClasses[TPBool] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPBool] };
            ValueClasses[TPAction] = new NPrimitiveType(this) { NTemplateClass = TemplateClasses[TPAction] };
            ValueClasses[DefaultObjTemplate] = new NAppClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public NValueClass FindValueClass(Template template) {
            template = GetPrototype(template);
            return ValueClasses[template];
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NMetadataClass.</returns>
        public NMetadataClass FindMetaClass(Template template) {
            template = GetPrototype(template);
            return MetaClasses[template];
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
        /// <returns>NTemplateClass.</returns>
        public NTemplateClass FindTemplateClass(Template template) {
            // template = GetPrototype(template);
            return TemplateClasses[template];
        }

        internal TString TPString = new TString();
        internal TLong TPLong = new TLong();
        internal TDecimal TPDecimal = new TDecimal();
        internal TObj DefaultObjTemplate = null;
        internal TDouble TPDouble = new TDouble();
        internal TBool TPBool = new TBool();
        internal TTrigger TPAction = new TTrigger();

        /// <summary>
        /// This is the main calling point to generate a dom tree for a application template.
        /// </summary>
        /// <param name="at">The App template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public NRoot GenerateDomTree(TObj at, CodeBehindMetadata metadata) {
            var root = new NRoot(this);
            var acn = new NAppClass(this) {
                Parent = root,
                IsPartial = true
            };

            var tcn = new NTAppClass(this) {
                Parent = acn,
                NValueClass = acn,
                Template = at,
                _Inherits = DefaultObjTemplate.GetType().Name, // "TPuppet,TJson",
                AutoBindProperties = metadata.AutoBindToDataObject
            };

            if (metadata == CodeBehindMetadata.Empty) { 
                // No codebehind. Need to set a few extra properties depending on metadata from json
                
                acn.IsPartial = false;
                acn._Inherits = DefaultObjTemplate.InstanceType.Name;

                // A specific IBindable type have been specified in the json.
                // We add it as a generic value on the Json-class this class inherits from.
                if (at.InstanceDataTypeName != null) {
                    acn._Inherits += '<' + at.InstanceDataTypeName + '>';
                    tcn.AutoBindProperties = true;
                }
            }

            var mcn = new NObjMetadata(this) {
                Parent = acn,
                NTemplateClass = tcn,
                _Inherits = "ObjMetadata"
            };

            tcn.NMetadataClass = mcn;

            this.ValueClasses[at] = acn;
            this.TemplateClasses[at] = tcn;
            this.MetaClasses[at] = mcn;

            root.AppClassClassNode = acn;
            acn.NTemplateClass = tcn;
            GenerateKids(acn,
                        (NTAppClass)acn.NTemplateClass,
                        acn.NTemplateClass.NMetadataClass,
                        acn.NTemplateClass.Template);
            MoveNestedClassToBottom(root);

            if (metadata != CodeBehindMetadata.Empty) {
                var json = new NJsonAttributeClass(this) {
                    Parent = acn,
                    IsStatic = true,
                    _Inherits = null,
                    _ClassName = "json"
                };
                GenerateJsonAttributes(acn, json);

                var input = new NOtherClass(this) {
                    Parent = acn,
                    _ClassName = "Input",
                    IsStatic = true
                };
                GeneratePrimitiveValueEvents(input, acn, "Input");

                ConnectCodeBehindClasses(root, metadata);
                GenerateInputBindings((NTAppClass)acn.NTemplateClass, metadata);
            }
            CheckMissingBindingInformation(tcn);

            return root;
        }

        private void CheckMissingBindingInformation(NTAppClass ntApp) {
            NArrXXXClass tArr;
            NTAppClass childTApp;
            NProperty property;
            string propertyName;

            if (!ntApp.AutoBindProperties)
                return;

            foreach (NBase nb in ntApp.Children) {
                property = nb as NProperty;
                if (property != null) {
                    propertyName = property.Template.PropertyName;
                    if (string.IsNullOrEmpty(propertyName) || propertyName[0] == '_')
                        continue;

                    tArr = property.Type as NArrXXXClass;
                    if (tArr != null)
                        childTApp = (NTAppClass)tArr.NTApp;
                    else
                        childTApp = property.Type as NTAppClass;

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
                            throw ErrorCode.ToException(Error.SCERRMISSINGDATATYPEBINDINGJSON, "Path: '" + propertyName + "'");
                        }
                        CheckMissingBindingInformation(childTApp);
                    }
                }
            }
        }

        /// <summary>
        /// Connects the code behind classes.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="metadata">The metadata.</param>
        private void ConnectCodeBehindClasses(NRoot root, CodeBehindMetadata metadata) {
            TObj appTemplate;
            TObj rootTemplate;
            TObj[] classesInOrder;
            JsonMapInfo mapInfo;
            NAppClass nAppClass;

            classesInOrder = new TObj[metadata.JsonPropertyMapList.Count];
            rootTemplate = root.AppClassClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                mapInfo = metadata.JsonPropertyMapList[i];

                appTemplate = FindTAppFor(mapInfo.JsonMapName, rootTemplate);

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

                nAppClass = ValueClasses[appTemplate] as NAppClass;
                nAppClass.IsPartial = true;
                nAppClass._Inherits = null;

                if (mapInfo.AutoBindToDataObject) {
                    var ntAppClass = TemplateClasses[appTemplate] as NTAppClass;
                    ntAppClass.AutoBindProperties = true;
                }

                classesInOrder[i] = appTemplate;
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
                                              List<JsonMapInfo> mapInfos,
                                              NRoot root) {
            List<String> parentClasses;
            NBase parent;
            NClass theClass;
            NClass parentClass;
            NOtherClass notExistingClass;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                theClass = ValueClasses[classesInOrder[i]];
                parentClasses = mapInfos[i].ParentClasses;
                if (parentClasses.Count > 0) {
                    parent = root;
                    for (Int32 pi = parentClasses.Count - 1; pi >= 0; pi--) {
                        parentClass = FindClass(parentClasses[pi], parent);
                        if (parentClass != null) {
                            parent = parentClass;
                        } else {
                            notExistingClass = new NOtherClass(this);
                            notExistingClass._ClassName = parentClasses[pi];
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

        /// <summary>
        /// Finds the class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>NClass.</returns>
        private NClass FindClass(String className, NBase parent) {
            NClass nClass;
            for (Int32 i = 0; i < parent.Children.Count; i++) {
                nClass = parent.Children[i] as NClass;
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
        private TObj FindTAppFor(String jsonMapName, TObj rootTemplate) {
            TObj appTemplate;
            String[] mapParts;
            Template template;

            appTemplate = rootTemplate;
            mapParts = jsonMapName.Split('.');

            // We skip the two first parts since the first one will always be "json" 
            // and the second the rootTemplate.
            for (Int32 i = 1; i < mapParts.Length; i++) {
                template = appTemplate.Properties.GetTemplateByPropertyName(mapParts[i]);
                if (template is TObj) {
                    appTemplate = (TObj)template;
                } else if (template is TObjArr) {
                    appTemplate = ((TObjArr)template).App;
                } else {
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
        private void MoveNestedClassToBottom(NBase node) {
            var move = new List<NBase>();
            foreach (var kid in node.Children) {
                if (kid is NAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is NTAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is NObjMetadata) {
                    move.Add(kid);
                }
            }
            foreach (var kid in move) {
                kid.Parent = kid.Parent;
                MoveNestedClassToBottom(kid);
            }
        }

        /// <summary>
        /// Generates the kids.
        /// </summary>
        /// <param name="appClassParent">The app class parent.</param>
        /// <param name="templParent">The templ parent.</param>
        /// <param name="metaParent">The meta parent.</param>
        /// <param name="template">The template.</param>
        /// <exception cref="System.Exception"></exception>
        private void GenerateKids(NAppClass appClassParent,
                                  NTAppClass templParent,
                                  NClass metaParent,
                                  Template template) {
            if (template is TContainer) {
                var pt = (TContainer)template;
                foreach (var kid in pt.Children) {
                    if (kid is TContainer) {
                        if (kid is TObj) {
                            GenerateForApp(kid as TObj,
                                           appClassParent,
                                           templParent,
                                           metaParent,
                                           template);
                        } else if (kid is TObjArr) {
                            // Orphaned by design as primitive types dont get custom template classes
//                            var type = new NArrXXXClass(NValueClass.Classes[kid.InstanceType] ) { Template = kid }; 
//                            NTemplateClass.Classes[kid] = type;

                            GenerateForArr(kid as TObjArr,
                                               appClassParent,
                                               templParent,
                                               metaParent,
                                               template);
                        } else {
                            throw new Exception();
                        }
                    } else {
                        // Orphaned by design as primitive types dont get custom template classes
                        var type = new NPropertyClass(this) { Template = kid /*, Parent = appClassParent */ }; 
                        TemplateClasses[kid] = type;

                        GenerateProperty(kid, appClassParent, templParent, metaParent);
                    }
                }
            }
        }

        /// <summary>
        /// Generates the ast nodes for a custom app class and the corresponding
        /// app template and app meta data custom classes.
        /// </summary>
        /// <param name="at">At.</param>
        /// <param name="appClassParent">The app class parent.</param>
        /// <param name="templParent">The templ parent.</param>
        /// <param name="metaParent">The meta parent.</param>
        /// <param name="template">The template.</param>
        /// <exception cref="System.Exception"></exception>
        private void GenerateForApp(TObj at,
                                    NAppClass appClassParent,
                                    NTAppClass templParent,
                                    NClass metaParent,
                                    Template template) {
            NValueClass acn;
            NTemplateClass tcn;
            NMetadataClass mcn;
            if (at.Properties.Count == 0) {
                // Empty App templates does not typically receive a custom template 
                // class (unless explicitly set by the Json.nnnn syntax (TODO)
                // This means that they can be assigned to any App object. 
                // A typical example is to have a Page:{} property in a master
                // app (representing, for example, child web pages)
                acn = ValueClasses[DefaultObjTemplate];
                tcn = TemplateClasses[DefaultObjTemplate];
                mcn = MetaClasses[DefaultObjTemplate];
            } else {
                NAppClass racn;
                acn = racn = new NAppClass(this) {
                    Parent = appClassParent,
                    _Inherits = DefaultObjTemplate.InstanceType.Name // "Puppet", "Json"
                };

                tcn = new NTAppClass(this) {
                    Parent = racn,
                    Template = at,
                    NValueClass = racn,
                    _Inherits = DefaultObjTemplate.GetType().Name // "TPuppet", "TJson"
                };

                // A specific IBindable type have been specified in the json.
                // We add it as a generic value on the Json-class this class inherits from.
                if (at.InstanceDataTypeName != null) {
                    racn._Inherits += '<' + at.InstanceDataTypeName + '>';
                    ((NTAppClass)tcn).AutoBindProperties = true;
                }

                mcn = new NObjMetadata(this) {
                    Parent = racn,
                    NTemplateClass = tcn,
                    _Inherits = "ObjMetadata"
                };
                tcn.NMetadataClass = mcn;
                racn.NTemplateClass = tcn;

                GenerateKids(acn as NAppClass,
                             tcn as NTAppClass,
                             mcn as NObjMetadata,
                             at);

                if (!appClassParent.Children.Remove(acn))
                    throw new Exception(); // Move to...
                appClassParent.Children.Add(acn); // Move to...
                if (!acn.Children.Remove(tcn))
                    throw new Exception(); // Move to...
                acn.Children.Add(tcn); // Move to...
                if (!acn.Children.Remove(mcn))
                    throw new Exception(); // ...last member
                acn.Children.Add(mcn); // ...last member
            }
            ValueClasses[at] = acn;
            TemplateClasses[at] = tcn;
            MetaClasses[at] = mcn;

            if (at.Parent is TObj)
                GenerateProperty(at, appClassParent, templParent, metaParent);
        }

        private NBase FindRootNAppClass(NAppClass appClassParent) {
            NBase next = appClassParent;
            while (!(next.Parent is NRoot))
                next = next.Parent;
            return next;
        }

        /// <summary>
        /// Generates the property.
        /// </summary>
        /// <param name="at">At.</param>
        /// <param name="appClassParent">The app class parent.</param>
        /// <param name="templParent">The templ parent.</param>
        /// <param name="metaParent">The meta parent.</param>
        private void GenerateProperty(Template at,
                                      NAppClass appClassParent,
                                      NTAppClass templParent,
                                      NClass metaParent) {
            var valueClass = FindValueClass(at);
            var type = FindTemplateClass(at);

            type.NValueProperty = new NProperty(this) {
                Parent = appClassParent,
                Template = at,
                Type = valueClass
            };
            new NProperty(this) {
                Parent = templParent,
                Template = at,
                Type = type
            };
            new NProperty(this) {
                Parent = templParent.Constructor,
                Template = at,
                Type = FindTemplateClass(at)
            };
            new NProperty(this) {
                Parent = metaParent,
                Template = at,
                Type = FindMetaClass(at)
            };
        }

        /// <summary>
        /// Generates for listing.
        /// </summary>
        /// <param name="alt">The alt.</param>
        /// <param name="appClassParent">The app class parent.</param>
        /// <param name="templParent">The templ parent.</param>
        /// <param name="metaParent">The meta parent.</param>
        /// <param name="template">The template.</param>
        private void GenerateForArr(TObjArr alt,
                                        NAppClass appClassParent,
                                        NTAppClass templParent,
                                        NClass metaParent,
                                        Template template) {
            var amn = new NProperty(this) {
                Parent = appClassParent,
                Template = alt
            };
            var tmn = new NProperty(this) {
                Parent = appClassParent.NTemplateClass,
                Template = alt
            };
            var cstmn = new NProperty(this) {
                Parent = ((NTAppClass)appClassParent.NTemplateClass).Constructor,
                Template = alt
            };
            var mmn = new NProperty(this) {
                Parent = appClassParent.NTemplateClass.NMetadataClass,
                Template = alt
            };
            GenerateKids(appClassParent, templParent, metaParent, alt);
            var vlist = new NArrXXXClass(this, "Arr", ValueClasses[alt.App], null, alt);
            amn.Type = vlist;

            tmn.Type = new NArrXXXClass(this, "TArr",
                                            ValueClasses[alt.App],
                                            TemplateClasses[alt.App], alt);
            cstmn.Type = new NArrXXXClass(this, "TArr",
                                            ValueClasses[alt.App],
                                            TemplateClasses[alt.App], alt);

            mmn.Type = new NArrXXXClass(this, "ArrMetadata",
                                            ValueClasses[alt.App],
                                            TemplateClasses[alt.App], alt);

            //ntempl.Template = alt;
            //            NTemplateClass.Classes[alt] = tlist;
            ValueClasses[alt] = vlist;
        }

        /// <summary>
        /// The JSON attributes is a set of source code attributes (C# Attributes)
        /// used to annotate which user classes should be used for which JSON tree
        /// nodes (objects). This allows the user to write classes that are not deeply
        /// nested (unless he/she wants the class declarations nested). The function
        /// is recursive and calls itself.
        /// </summary>
        /// <param name="appClass">The node to generate attributes for</param>
        /// <param name="parent">The DOM node to generate attributes for</param>
        public void GenerateJsonAttributes(NAppClass appClass, NBase parent) {
            foreach (var kid in appClass.Children) {
                if (kid is NAppClass) {
                    var x = new NJsonAttributeClass(this) {
                        _Inherits = "TemplateAttribute",
                        _ClassName = (kid as NAppClass).Stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes(kid as NAppClass, x);
                }
            }
        }

        /// <summary>
        /// Used to generate Handle( ... ) event classes used by the user programmer
        /// to catch events such as the Input event.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="app">The app.</param>
        /// <param name="eventName">The name of the event (i.e. "Input").</param>
        public void GeneratePrimitiveValueEvents(NBase parent, NClass app, string eventName) {
            foreach (var kid in app.Children) {
                if (kid is NProperty) {
                    var mn = kid as NProperty;
                    if (mn.Type is NArrXXXClass ||
                       (mn.Type is NAppClass && mn.Type.Children.Count > 0)) {
                        NClass type;
                        if (mn.Type is NArrXXXClass)
                            type = (mn.Type as NArrXXXClass).NApp;
                        else
                            type = mn.Type as NAppClass;
                        var x = new NOtherClass(this) {
                            Parent = parent,
                            IsStatic = true,
                            _ClassName = mn.MemberName
                        };
                        GeneratePrimitiveValueEvents(x, type, eventName);
                    } else {
                        if (mn.Type is NPrimitiveType) {
                            new NEventClass(this) {
                                NMember = mn,
                                Parent = parent,
                                //                                NApp = app,
                                EventName = eventName
                            };
                        }
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
        private void GenerateInputBindings(NTAppClass nApp,
                                           CodeBehindMetadata metadata) {
            Int32 index;
            List<NBase> children;
            NTAppClass propertyAppClass;
            NConstructor cst;
            NInputBinding binding;
            NProperty np;
            Template template;
            String propertyName;
            String[] parts;

            foreach (InputBindingInfo info in metadata.InputBindingList) {
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
                    np = (NProperty)children.Find((NBase child) => {
                                        NProperty property = child as NProperty;
                                        if (property != null)
                                            return property.Template.PropertyName.Equals(parts[i]);
                                        return false;
                                    });

                    template = np.Template;
                    if (template is TObjArr) {
                        template = ((TObjArr)template).App;
                    }
                    propertyAppClass = (NTAppClass)FindTemplateClass(template);
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
                index = children.FindIndex((NBase child) => {
                            NProperty property = child as NProperty;
                            if (property != null)
                                return property.MemberName.Equals(propertyName);
                            return false;
                        });

                if (index == -1) {
                    // TODO:
                    // Need to add file and linenumbers if possible to pinpoint the erroneous code.
                    throw new Exception("Invalid Handle-method declared in class "
                                        + info.DeclaringClassName
                                        + ". No property with name "
                                        + propertyName +
                                        " exists.");
                }

                binding = new NInputBinding(this);
                binding.BindsToProperty = (NProperty)cst.Children[index];
                binding.PropertyAppClass = (NAppClass)FindValueClass(propertyAppClass.Template);
                binding.InputTypeName = info.FullInputTypeName;
                FindHandleDeclaringClass(binding, info);

                // We check the next item in the constructor. All inputbindings for 
                // the same property needs to be ordered with the least parent-calls first.
                Int32 indexToCheck = index + 1;
                while (indexToCheck < children.Count) {
                    NInputBinding otherBinding = children[indexToCheck] as NInputBinding;

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

        /// <summary>
        /// Finds the class where the Handle method is declared. This can be the same class
        /// as where the property is declared or a parentclass.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="info">The info.</param>
        /// <exception cref="System.Exception">Could not find the app where Handle method is declared.</exception>
        private void FindHandleDeclaringClass(NInputBinding binding, InputBindingInfo info) {
            Int32 parentCount = 0;
            TContainer candidate = binding.PropertyAppClass.Template;
            TObj appTemplate;
            NAppClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TObj;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName)) {
                        declaringAppClass = (NAppClass)FindValueClass(appTemplate);
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
        /// Employed by the template code generator.
        /// </summary>
        /// <value>The global namespace.</value>
        internal string GlobalNamespace {
            get {
                Template current = DefaultObjTemplate;
                while (current.Parent != null)
                    current = (Template)current.Parent;
                return ((TObj)current).Namespace;
            }
        }
    }
}
