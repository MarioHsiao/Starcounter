// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {

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
    public class Gen2DomGenerator {
        internal Gen2DomGenerator(Gen2CodeGenerationModule mod, TObj template, Type defaultNewObjTemplateType) {
            DefaultObjTemplate = (TObj)defaultNewObjTemplateType.GetConstructor(new Type[0]).Invoke(null);
            InitTemplateClasses();
            InitMetadataClasses();
            InitValueClasses();
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstValueClass> ValueClasses = new Dictionary<Template, AstValueClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstTemplateClass> TemplateClasses = new Dictionary<Template, AstTemplateClass>();
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<Template, AstMetadataClass> MetaClasses = new Dictionary<Template, AstMetadataClass>();

        /// <summary>
        /// Initializes static members of the <see cref="AstTemplateClass" /> class.
        /// </summary>
        void InitTemplateClasses() {
            TemplateClasses[TPString] = new AstPropertyClass(this) { Template = TPString };
            TemplateClasses[TPLong] = new AstPropertyClass(this) { Template = TPLong };
            TemplateClasses[TPDecimal] = new AstPropertyClass(this) { Template = TPDecimal };
            TemplateClasses[TPDouble] = new AstPropertyClass(this) { Template = TPDouble };
            TemplateClasses[TPBool] = new AstPropertyClass(this) { Template = TPBool };
            TemplateClasses[TPAction] = new AstPropertyClass(this) { Template = TPAction };
            TemplateClasses[DefaultObjTemplate] = new AstTAppClass(this) { Template = DefaultObjTemplate };
        }

        /// <summary>
        /// Initializes static members of the <see cref="AstMetadataClass" /> class.
        /// </summary>
        void InitMetadataClasses() {
            MetaClasses[TPString] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPString] };
            MetaClasses[TPLong] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPLong] };
            MetaClasses[TPDecimal] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            MetaClasses[TPDouble] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPDouble] };
            MetaClasses[TPBool] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPBool] };
            MetaClasses[TPAction] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[TPAction] };
            MetaClasses[DefaultObjTemplate] = new AstMetadataClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Initializes static members of the <see cref="AstValueClass" /> class.
        /// </summary>
        void InitValueClasses() {
            ValueClasses[TPString] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPString] };
            ValueClasses[TPLong] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPLong] };
            ValueClasses[TPDecimal] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDecimal] };
            ValueClasses[TPDouble] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPDouble] };
            ValueClasses[TPBool] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPBool] };
            ValueClasses[TPAction] = new AstPrimitiveType(this) { NTemplateClass = TemplateClasses[TPAction] };
            ValueClasses[DefaultObjTemplate] = new AstAppClass(this) { NTemplateClass = TemplateClasses[DefaultObjTemplate] };
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NValueClass.</returns>
        public AstValueClass FindValueClass(Template template) {
            template = GetPrototype(template);
            return ValueClasses[template];
        }

        /// <summary>
        /// Finds the specified template.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>NMetadataClass.</returns>
        public AstMetadataClass FindMetaClass(Template template) {
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
        public AstTemplateClass FindTemplateClass(Template template) {
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
        public AstRoot GenerateDomTree(TObj at, CodeBehindMetadata metadata) {
            var root = new AstRoot(this);
            var acn = new AstAppClass(this) {
                Parent = root,
                IsPartial = true
            };

            var tcn = new AstTAppClass(this) {
                Parent = acn,
                NValueClass = acn,
                Template = at,
                _Inherits = DefaultObjTemplate.GetType().Name, // "TPuppet,TJson",
                AutoBindProperties = metadata.RootClassInfo.AutoBindToDataObject
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
            } else if (at.InstanceDataTypeName != null) {
                ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, at.CompilerOrigin);
            }

            var mcn = new AstObjMetadata(this) {
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
                        (AstTAppClass)acn.NTemplateClass,
                        acn.NTemplateClass.NMetadataClass,
                        acn.NTemplateClass.Template);
            MoveNestedClassToBottom(root);

            if (metadata != CodeBehindMetadata.Empty) {
                // if there is codebehind and the class is not inherited from Json we need 
                // to change the inheritance on the template and metadata classes as well.
                var tmp = metadata.RootClassInfo.BaseClassName;
                if (!string.IsNullOrEmpty(tmp) && !tmp.Equals("Json")) {
                    tcn._Inherits = "T" + metadata.RootClassInfo.BaseClassName;
                    mcn._Inherits = tmp + "Metadata";
                }

                var json = new AstJsonAttributeClass(this) {
                    Parent = acn,
                    IsStatic = true,
                    _Inherits = null,
                    _ClassName = "json"
                };
                GenerateJsonAttributes(acn, json);

                var input = new AstOtherClass(this) {
                    Parent = acn,
                    _ClassName = "Input",
                    IsStatic = true
                };
                GeneratePrimitiveValueEvents(input, acn, "Input");

                ConnectCodeBehindClasses(root, metadata);
                GenerateInputBindings((AstTAppClass)acn.NTemplateClass, metadata);
            }
           // CheckMissingBindingInformation(tcn);

            return root;
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
                            ThrowExceptionWithLineInfo(Error.SCERRMISSINGDATATYPEBINDINGJSON, "Path: '" + propertyName + "'", null, property.Template.CompilerOrigin);
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
                        ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, appTemplate.CompilerOrigin);
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

                    nAppClass = ValueClasses[appTemplate] as AstAppClass;
                    nAppClass.IsPartial = true;
                    nAppClass._Inherits = null;

                    var ntAppClass = TemplateClasses[appTemplate] as AstTAppClass;
                    var mdAppClass = MetaClasses[appTemplate] as AstObjMetadata;

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
            List<String> parentClasses;
            AstBase parent;
            AstClass theClass;
            AstClass parentClass;
            AstOtherClass notExistingClass;

            for (Int32 i = 0; i < classesInOrder.Length; i++) {
                var cls = classesInOrder[i];
                if (cls != null) {
                    theClass = ValueClasses[cls];
                    parentClasses = mapInfos[i].ParentClasses;
                    if (parentClasses.Count > 0) {
                        parent = root;
                        for (Int32 pi = parentClasses.Count - 1; pi >= 0; pi--) {
                            parentClass = FindClass(parentClasses[pi], parent);
                            if (parentClass != null) {
                                parent = parentClass;
                            }
                            else {
                                notExistingClass = new AstOtherClass(this);
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
        private AstClass FindClass(String className, AstBase parent) {
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
            String[] mapParts;
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

        /// <summary>
        /// Provide a nicer default order of the generated classes such
        /// that the primitive properties (string, int, etc.) comes first and
        /// the nested classes comes in the end of the class declaration.
        /// Show the nested App classes before the template and metadata classes.
        /// </summary>
        /// <param name="node">The node containing the children to rearrange</param>
        private void MoveNestedClassToBottom(AstBase node) {
            var move = new List<AstBase>();
            foreach (var kid in node.Children) {
                if (kid is AstAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstTAppClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children) {
                if (kid is AstObjMetadata) {
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
        private void GenerateKids(AstAppClass appClassParent,
                                  AstTAppClass templParent,
                                  AstClass metaParent,
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
                        var type = new AstPropertyClass(this) { Template = kid /*, Parent = appClassParent */ }; 
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
                                    AstAppClass appClassParent,
                                    AstTAppClass templParent,
                                    AstClass metaParent,
                                    Template template) {
            AstValueClass acn;
            AstTemplateClass tcn;
            AstMetadataClass mcn;
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
                AstAppClass racn;
                acn = racn = new AstAppClass(this) {
                    Parent = appClassParent,
                    _Inherits = DefaultObjTemplate.InstanceType.Name // "Puppet", "Json"
                };

                tcn = new AstTAppClass(this) {
                    Parent = racn,
                    Template = at,
                    NValueClass = racn,
                    _Inherits = DefaultObjTemplate.GetType().Name // "TPuppet", "TJson"
                };

                // A specific IBindable type have been specified in the json.
                // We add it as a generic value on the Json-class this class inherits from.
                if (at.InstanceDataTypeName != null) {
                    racn._Inherits += '<' + at.InstanceDataTypeName + '>';
                    ((AstTAppClass)tcn).AutoBindProperties = true;
                }

                mcn = new AstObjMetadata(this) {
                    Parent = racn,
                    NTemplateClass = tcn,
                    _Inherits = "ObjMetadata"
                };
                tcn.NMetadataClass = mcn;
                racn.NTemplateClass = tcn;

                GenerateKids(acn as AstAppClass,
                             tcn as AstTAppClass,
                             mcn as AstObjMetadata,
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

        private AstBase FindRootNAppClass(AstAppClass appClassParent) {
            AstBase next = appClassParent;
            while (!(next.Parent is AstRoot))
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
                                      AstAppClass appClassParent,
                                      AstTAppClass templParent,
                                      AstClass metaParent) {
            var valueClass = FindValueClass(at);
            var type = FindTemplateClass(at);

            type.NValueProperty = new AstProperty(this) {
                Parent = appClassParent,
                Template = at,
                Type = valueClass
            };
            new AstProperty(this) {
                Parent = templParent,
                Template = at,
                Type = type
            };
            new AstProperty(this) {
                Parent = templParent.Constructor,
                Template = at,
                Type = FindTemplateClass(at)
            };
            new AstProperty(this) {
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
                                        AstAppClass appClassParent,
                                        AstTAppClass templParent,
                                        AstClass metaParent,
                                        Template template) {
            var amn = new AstProperty(this) {
                Parent = appClassParent,
                Template = alt
            };
            var tmn = new AstProperty(this) {
                Parent = appClassParent.NTemplateClass,
                Template = alt
            };
            var cstmn = new AstProperty(this) {
                Parent = ((AstTAppClass)appClassParent.NTemplateClass).Constructor,
                Template = alt
            };
            var mmn = new AstProperty(this) {
                Parent = appClassParent.NTemplateClass.NMetadataClass,
                Template = alt
            };
            GenerateKids(appClassParent, templParent, metaParent, alt);
            var vlist = new AstArrXXXClass(this, "Arr", ValueClasses[alt.ElementType], null, alt);
            amn.Type = vlist;

            tmn.Type = new AstArrXXXClass(this, "TArr",
                                            ValueClasses[alt.ElementType],
                                            TemplateClasses[alt.ElementType], alt);
            cstmn.Type = new AstArrXXXClass(this, "TArr",
                                            ValueClasses[alt.ElementType],
                                            TemplateClasses[alt.ElementType], alt);

            mmn.Type = new AstArrXXXClass(this, "ArrMetadata",
                                            ValueClasses[alt.ElementType],
                                            TemplateClasses[alt.ElementType], alt);

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
        public void GenerateJsonAttributes(AstAppClass appClass, AstBase parent) {
            foreach (var kid in appClass.Children) {
                if (kid is AstAppClass) {
                    var x = new AstJsonAttributeClass(this) {
                        _Inherits = "TemplateAttribute",
                        _ClassName = (kid as AstAppClass).Stem,
                        Parent = parent

                    };
                    GenerateJsonAttributes(kid as AstAppClass, x);
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
        public void GeneratePrimitiveValueEvents(AstBase parent, AstClass app, string eventName) {
            foreach (var kid in app.Children) {
                if (kid is AstProperty) {
                    var mn = kid as AstProperty;
                    if (mn.Type is AstArrXXXClass ||
                       (mn.Type is AstAppClass && mn.Type.Children.Count > 0)) {
                        AstClass type;
                        if (mn.Type is AstArrXXXClass)
                            type = (mn.Type as AstArrXXXClass).NApp;
                        else
                            type = mn.Type as AstAppClass;
                        var x = new AstOtherClass(this) {
                            Parent = parent,
                            IsStatic = true,
                            _ClassName = mn.MemberName
                        };
                        GeneratePrimitiveValueEvents(x, type, eventName);
                    } else {
                        if (mn.Type is AstPrimitiveType) {
                            new AstEventClass(this) {
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
        private void GenerateInputBindings(AstTAppClass nApp,
                                           CodeBehindMetadata metadata) {
            Int32 index;
            List<AstBase> children;
            AstTAppClass propertyAppClass;
            AstConstructor cst;
            AstInputBinding binding;
            AstProperty np;
            Template template;
            String propertyName;
            String[] parts;

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
                    propertyAppClass = (AstTAppClass)FindTemplateClass(template);
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

                binding = new AstInputBinding(this);
                binding.BindsToProperty = (AstProperty)cst.Children[index];
                binding.PropertyAppClass = (AstAppClass)FindValueClass(propertyAppClass.Template);
                binding.InputTypeName = info.FullInputTypeName;
                FindHandleDeclaringClass(binding, info);

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

        /// <summary>
        /// Finds the class where the Handle method is declared. This can be the same class
        /// as where the property is declared or a parentclass.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="info">The info.</param>
        /// <exception cref="System.Exception">Could not find the app where Handle method is declared.</exception>
        private void FindHandleDeclaringClass(AstInputBinding binding, InputBindingInfo info) {
            Int32 parentCount = 0;
            TContainer candidate = binding.PropertyAppClass.Template;
            TObj appTemplate;
            AstAppClass declaringAppClass = null;

            while (candidate != null) {
                appTemplate = candidate as TObj;
                if (appTemplate != null) {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName)) {
                        declaringAppClass = (AstAppClass)FindValueClass(appTemplate);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="messagePostFix"></param>
        /// <param name="innerException"></param>
        /// <param name="co"></param>
        private static void ThrowExceptionWithLineInfo(uint errorCode, string messagePostFix, Exception innerException, CompilerOrigin co) {
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
