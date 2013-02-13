﻿// ***********************************************************************
// <copyright file="DomGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Starcounter.Templates.Interfaces;
using System.CodeDom.Compiler;

[assembly: InternalsVisibleTo("Starcounter.Application.Test")]

namespace Starcounter.Internal.Application.CodeGeneration
{

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
    public class DomGenerator
    {

        /// <summary>
        /// Creates a dom generator for a template
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="template">The represented template</param>
        internal DomGenerator(CodeGenerationModule mod, Template template)
        { //, string typename, string templateClass, string metadataClass ) {
            Template = template;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a application template.
        /// </summary>
        /// <param name="at">The App template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public NRoot GenerateDomTree(AppTemplate at, CodeBehindMetadata metadata )
        {
            var root = new NRoot();
            var acn = new NAppClass()
            {
                Parent = root,
                IsPartial = true,
                AutoBindPropertiesToEntity = metadata.AutoBindToEntity,
                GenericTypeArgument = metadata.GenericArgument
            };

            var tcn = new NAppTemplateClass()
            {
                Parent = acn,
                NValueClass = acn,
                Template = at,
                _Inherits = "AppTemplate"
            };
            var mcn = new NAppMetadata()
            {
                Parent = acn,
                NTemplateClass = tcn,
                _Inherits = "AppMetadata"
            };

            new NAppSerializerClass() {
                Parent = acn,
                NAppClass = acn
            };

//            acn.NTemplateClass.Temp               NTemplateClass = NTemplateClass.Classes[at],
            tcn.NMetadataClass = mcn;

            NValueClass.Classes[at] = acn;
            NTemplateClass.Classes[at] = tcn;
            NMetadataClass.Classes[at] = mcn;

            root.AppClassClassNode = acn;
//            acn.MetaDataClass = mcn;
            acn.NTemplateClass = tcn;
//            if (acn is NAppClass) {
//                var racn = acn as NAppClass;
            GenerateKids(acn,                         
                        (NAppTemplateClass)acn.NTemplateClass, 
                        acn.NTemplateClass.NMetadataClass, 
                        acn.NTemplateClass.Template);
//            }

//            tcn.Parent = tcn.Parent;
//            mcn.Parent = mcn.Parent;
            MoveNestedClassToBottom(root);

//                Container = acn,
            var json = new NJsonAttributeClass()
            {
                Parent = acn,
                IsStatic = true,
                _Inherits = null,
                _ClassName = "Json"
            };
            GenerateJsonAttributes(acn, json);

            var input = new NOtherClass()
            {
                Parent = acn,
                _ClassName = "Input",
                IsStatic = true
            };
            GeneratePrimitiveValueEvents(input, acn, "Input");

            ConnectCodeBehindClasses(root, metadata);
            GenerateInputBindings((NAppTemplateClass)acn.NTemplateClass, metadata);
            MoveSerializersToBottom(acn);
            return root;
        }

        /// <summary>
        /// Connects the code behind classes.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="metadata">The metadata.</param>
        private void ConnectCodeBehindClasses(NRoot root, CodeBehindMetadata metadata)
        {
            ObjTemplate appTemplate;
            ObjTemplate rootTemplate;
            ObjTemplate[] classesInOrder;
            JsonMapInfo mapInfo;
            NAppClass nAppClass;
            NTemplateClass nTemplateclass;

            classesInOrder = new AppTemplate[metadata.JsonPropertyMapList.Count];
            rootTemplate = root.AppClassClassNode.Template;

            for (Int32 i = 0; i < classesInOrder.Length; i++)
            {
                mapInfo = metadata.JsonPropertyMapList[i];

                appTemplate = FindAppTemplateFor(mapInfo.JsonMapName, rootTemplate);

                // TODO:
                // If we have an empty object declaration in the jsonfile and
                // set an codebehind class on that property the AppTemplate here 
                // will be a generic empty one, which (I guess?) cannot be updated
                // here.
                // Do we need to create a new AppTemplate and replace the existing one
                // for all NClasses?
              
                appTemplate.ClassName = mapInfo.ClassName;
                if (!String.IsNullOrEmpty(mapInfo.Namespace))
                    appTemplate.Namespace = mapInfo.Namespace;

                nAppClass = NValueClass.Classes[appTemplate] as NAppClass;
                nAppClass.IsPartial = true;
                nAppClass._Inherits = null;
                nAppClass.AutoBindPropertiesToEntity = mapInfo.AutoBindToEntity;

                if (mapInfo.AutoBindToEntity) {
                    nAppClass.GenericTypeArgument = mapInfo.GenericArgument;
                    BindAutoBoundProperties(nAppClass.Children);
                    nTemplateclass = NAppTemplateClass.Classes[appTemplate];
                    BindAutoBoundProperties(nTemplateclass.Children);
                }

                classesInOrder[i] = appTemplate;
            }

            ReorderCodebehindClasses(classesInOrder, metadata.JsonPropertyMapList, root);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="children"></param>
        private void BindAutoBoundProperties(List<NBase> children) {
            NProperty property;

            foreach (NBase child in children) {
                if (child is NConstructor) {
                    BindAutoBoundProperties(child.Children);
                    continue;
                }

                property = child as NProperty;
                if (property != null) {
                    if ((property.MemberName == null) 
                        || (property.MemberName[0] == '_')
                        || (property.Template is ActionProperty)) {
                        continue;
                    }

                    property.Bound = true;
                }
            }
        }

        /// <summary>
        /// Reorders the codebehind classes.
        /// </summary>
        /// <param name="classesInOrder">The classes in order.</param>
        /// <param name="mapInfos">The map infos.</param>
        /// <param name="root">The root.</param>
        private void ReorderCodebehindClasses(ObjTemplate[] classesInOrder,
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
                theClass = NValueClass.Classes[classesInOrder[i]];
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

        /// <summary>
        /// Finds the class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="parent">The parent.</param>
        /// <returns>NClass.</returns>
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

        /// <summary>
        /// Finds the app template for.
        /// </summary>
        /// <param name="jsonMapName">Name of the json map.</param>
        /// <param name="rootTemplate">The root template.</param>
        /// <returns>AppTemplate.</returns>
        /// <exception cref="System.Exception">Invalid property to bind codebehind.</exception>
        private ObjTemplate FindAppTemplateFor(String jsonMapName, ObjTemplate rootTemplate)
        {
            ObjTemplate appTemplate;
            String[] mapParts;
            Template template;

            appTemplate = rootTemplate;
            mapParts = jsonMapName.Split('.');

            // We skip the two first parts since the first one will always be "Json" 
            // and the second the rootTemplate.
            for (Int32 i = 1; i < mapParts.Length; i++)
            {
                template = appTemplate.Properties.GetTemplateByPropertyName(mapParts[i]);
                if (template is AppTemplate)
                {
                    appTemplate = (AppTemplate)template;
                }
                else if (template is ObjArrProperty)
                {
                    appTemplate = ((ObjArrProperty)template).App;
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
            foreach (var kid in node.Children)
            {
                if (kid is NAppClass)
                {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children)
            {
                if (kid is NAppTemplateClass)
                {
                    move.Add(kid);
                }
            }
            foreach (var kid in node.Children)
            {
                if (kid is NAppMetadata)
                {
                    move.Add(kid);
                }
            }
            foreach (var kid in move)
            {
                kid.Parent = kid.Parent;
                MoveNestedClassToBottom(kid);
            }
        }

        /// <summary>
        /// Provide a nicer default order of the generated. Puts all serializer 
        /// classes in the end of the file.
        /// </summary>
        /// <param name="node">The node containing the children to rearrange</param>
        private void MoveSerializersToBottom(NBase node) {
            var move = new List<NBase>();
            foreach (var kid in node.Children) {
                if (kid is NAppSerializerClass) {
                    move.Add(kid);
                }
            }
            foreach (var kid in move) {
                kid.Parent = node;
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
                                  NAppTemplateClass templParent,
                                  NClass metaParent,
                                  Template template)
        {
            if (template is ParentTemplate)
            {
                var pt = (ParentTemplate)template;
                foreach (var kid in pt.Children)
                {
                    if (kid is ParentTemplate)
                    {
                        if (kid is AppTemplate)
                        {
                            GenerateForApp(kid as AppTemplate,
                                           appClassParent,
                                           templParent,
                                           metaParent,
                                           template);
                        }
                        else if (kid is ObjArrProperty)
                        {
//                            var type = new NListingXXXClass(NValueClass.Classes[kid.InstanceType] ) { Template = kid }; // Orphaned by design as primitive types dont get custom template classes
//                            NTemplateClass.Classes[kid] = type;

                            GenerateForListing(kid as ObjArrProperty,
                                               appClassParent,
                                               templParent,
                                               metaParent,
                                               template);
                        }
                        else
                        {
                            throw new Exception();
                        }
                    }
                    else
                    {
                        var type = new NPropertyClass() { Template = kid /*, Parent = appClassParent */ }; // Orphaned by design as primitive types dont get custom template classes
                        NTemplateClass.Classes[kid] = type;

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
        private void GenerateForApp(AppTemplate at,
                                    NAppClass appClassParent,
                                    NAppTemplateClass templParent,
                                    NClass metaParent,
                                    Template template)
        {
            NValueClass acn;
            NTemplateClass tcn;
            NMetadataClass mcn;
            if (at.Properties.Count == 0)
            {
                // Empty App templates does not typically receive a custom template 
                // class (unless explicitly set by the Json.nnnn syntax (TODO)
                // This means that they can be assigned to any App object. 
                // A typical example is to have a Page:{} property in a master
                // app (representing, for example, child web pages)
                acn = NValueClass.Classes[NTemplateClass.AppTemplate];
                tcn = NTemplateClass.Classes[NTemplateClass.AppTemplate];
                mcn = NMetadataClass.Classes[NTemplateClass.AppTemplate];
            }
            else
            {
                NAppClass racn;
                acn = racn = new NAppClass()
                {
                    Parent = appClassParent,
                    _Inherits = "App"
                };
                tcn = new NAppTemplateClass()
                {
                    Parent = racn,
                    Template = at,
                    NValueClass = racn,
                    _Inherits = "AppTemplate",
                };
                mcn = new NAppMetadata()
                {
                    Parent = racn,
                    NTemplateClass = tcn,
                    _Inherits = "AppMetadata"
                };
                tcn.NMetadataClass = mcn;
                racn.NTemplateClass = tcn;

                new NAppSerializerClass() {
                    Parent = FindRootNAppClass(appClassParent),
                    NAppClass = racn
                };

                GenerateKids(acn as NAppClass, 
                             tcn as NAppTemplateClass, 
                             mcn as NAppMetadata, 
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
            NValueClass.Classes[at] = acn;
            NTemplateClass.Classes[at] = tcn;
            NMetadataClass.Classes[at] = mcn;

            if (at.Parent is AppTemplate)
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
                                      NAppTemplateClass templParent,
                                      NClass metaParent)
        {
            // TODO: 
            // How do we set notbound on an autobound property?
            bool bound = false;
            if (!(at is ActionProperty))
            {
                bound = (at.Bound || (appClassParent.AutoBindPropertiesToEntity));
            }

            var valueClass = NValueClass.Find(at);
            var type = NTemplateClass.Find(at);

            type.NValueProperty = new NProperty()
            {
                Parent = appClassParent,
                Template = at,
                Type = valueClass,
                Bound = bound
            };
            new NProperty()
            {
                Parent = templParent,
                Template = at,
                Type = type,
                Bound = bound
            };
            new NProperty()
            {
                Parent = templParent.Constructor,
                Template = at,
                Type = NTemplateClass.Find(at),
                Bound = bound

            };
            new NProperty()
            {
                Parent = metaParent,
                Template = at,
                Type = NMetadataClass.Find(at),
                Bound = bound
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
        private void GenerateForListing(ObjArrProperty alt, 
                                        NAppClass appClassParent, 
                                        NAppTemplateClass templParent, 
                                        NClass metaParent, 
                                        Template template)
        {
            // TODO: 
            // How do we set notbound on an autobound property?
            bool bound = (alt.Bound || (appClassParent.AutoBindPropertiesToEntity));
            
            var amn = new NProperty()
            {
                Parent = appClassParent,
                Template = alt,
                Bound = bound
            };
            var tmn = new NProperty()
            {
                Parent = appClassParent.NTemplateClass,
                Template = alt,
                Bound = bound
            };
            var cstmn = new NProperty()
            {
                Parent = ((NAppTemplateClass)appClassParent.NTemplateClass).Constructor,
                Template = alt,
                Bound = bound
            };
            var mmn = new NProperty()
            {
                Parent = appClassParent.NTemplateClass.NMetadataClass,
                Template = alt,
                Bound = bound
            };
            GenerateKids(appClassParent, templParent, metaParent, alt);
            var vlist = new NListingXXXClass("Listing", NValueClass.Classes[alt.App], null,alt);
            amn.Type = vlist;

            tmn.Type = new NListingXXXClass("ArrProperty", 
                                            NValueClass.Classes[alt.App], 
                                            NTemplateClass.Classes[alt.App], alt);
            cstmn.Type = new NListingXXXClass("ArrProperty",
                                            NValueClass.Classes[alt.App],
                                            NTemplateClass.Classes[alt.App], alt);

            mmn.Type = new NListingXXXClass("ArrMetadata", 
                                            NValueClass.Classes[alt.App], 
                                            NTemplateClass.Classes[alt.App], alt);

            //ntempl.Template = alt;
//            NTemplateClass.Classes[alt] = tlist;
            NValueClass.Classes[alt] = vlist;
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
        public void GenerateJsonAttributes(NAppClass appClass, NBase parent)
        {
            foreach (var kid in appClass.Children)
            {
                if (kid is NAppClass)
                {
                    var x = new NJsonAttributeClass()
                    {
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
        public void GeneratePrimitiveValueEvents(NBase parent, NClass app, string eventName)
        {
            foreach (var kid in app.Children)
            {
                if (kid is NProperty)
                {
                    var mn = kid as NProperty;
                    if (mn.Type is NListingXXXClass || 
                       (mn.Type is NAppClass && mn.Type.Children.Count > 0))
                    {
                        NClass type;
                        if (mn.Type is NListingXXXClass)
                            type = (mn.Type as NListingXXXClass).NApp;
                        else
                            type = mn.Type as NAppClass;
                        var x = new NOtherClass()
                        {
                            Parent = parent,
                            IsStatic = true,
                            _ClassName = mn.MemberName
                        };
                        GeneratePrimitiveValueEvents(x, type, eventName);
                    }
                    else
                    {
                        if (mn.Type is NPrimitiveType)
                        {
                            new NEventClass()
                            {
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
        private void GenerateInputBindings(NAppTemplateClass nApp, 
                                           CodeBehindMetadata metadata)
        {
            Int32 index;
            List<NBase> children;
            NAppTemplateClass propertyAppClass;
            NConstructor cst;
            NInputBinding binding;
            NProperty np;
            Template template;
            String propertyName;
            String[] parts;

            foreach (InputBindingInfo info in metadata.InputBindingList)
            {
                // Find the property the binding is for. 
                // Might not be the same class as the one specified in the info object
                // since the Handle-implementation can be declared in a parent class.
                parts = info.FullInputTypeName.Split('.');
                propertyAppClass = nApp;
                
                // The first index is always "Input", the next ones until the last is
                // the childapps (the toplevel app is sent as a parameter in here)
                // and the last index is the name of the property.
                for (Int32 i = 1; i < parts.Length - 1; i++)
                {
                    children = propertyAppClass.Children;
                    np = (NProperty)children.Find((NBase child) =>
                                    {
                                        NProperty property = child as NProperty;   
                                        if (property != null)
                                            return property.Template.PropertyName.Equals(parts[i]);
                                        return false;
                                    });

                    template = np.Template;
                    if (template is ObjArrProperty)
                    {
                        template = ((ObjArrProperty)template).App;
                    }
                    propertyAppClass = (NAppTemplateClass)NAppTemplateClass.Find(template);
                }

                propertyName = parts[parts.Length-1];

                // We need to search in the children in the constructor since
                // other inputbindings might have been added that makes the
                // insertion index change.
                cst = propertyAppClass.Constructor;
                children = cst.Children;

                // TODO: 
                // Make sure that the inputbindings are added in the correct order if 
                // we have more than one handler for the same property.
                index = children.FindIndex((NBase child) =>
                        {
                            NProperty property = child as NProperty;
                            if (property != null)
                                return property.MemberName.Equals(propertyName);
                            return false;
                        });

                if (index == -1)
                {
                    // TODO:
                    // Need to add file and linenumbers if possible to pinpoint the erroneous code.
                    throw new Exception("Invalid Handle-method declared in class " 
                                        + info.DeclaringClassName 
                                        + ". No property with name " 
                                        + propertyName + 
                                        " exists.");
                }

                binding = new NInputBinding();
                binding.BindsToProperty = (NProperty)cst.Children[index];
                binding.PropertyAppClass = (NAppClass)NAppClass.Find(propertyAppClass.Template);
                binding.InputTypeName = info.FullInputTypeName;
                FindHandleDeclaringClass(binding, info);

                // We check the next item in the constructor. All inputbindings for 
                // the same property needs to be ordered with the least parent-calls first.
                Int32 indexToCheck = index + 1;
                while (indexToCheck < children.Count)
                {
                    NInputBinding otherBinding = children[indexToCheck] as NInputBinding;

                    if (otherBinding == null) break;
                    if (otherBinding.BindsToProperty != binding.BindsToProperty) break;

                    // Two handlers (or more) are declared for the same property. Lets
                    // order them with the least parentcalls first.
                    if (binding.AppParentCount < otherBinding.AppParentCount) break;

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
        private static void FindHandleDeclaringClass(NInputBinding binding, InputBindingInfo info)
        {
            Int32 parentCount = 0;
            ParentTemplate candidate = binding.PropertyAppClass.Template;
            AppTemplate appTemplate;
            NAppClass declaringAppClass = null;

            while (candidate != null)
            {
                appTemplate = candidate as AppTemplate;
                if (appTemplate != null)
                {
                    if (info.DeclaringClassName.Equals(appTemplate.ClassName))
                    {
                        declaringAppClass = (NAppClass)NAppClass.Find(appTemplate);
                        break;
                    }
                }

                candidate = candidate.Parent;
                parentCount++;
            }

            if (declaringAppClass == null)
            {
                throw new Exception("Could not find the app where Handle method is declared.");
            }

            binding.DeclaringAppClass = declaringAppClass;
            binding.AppParentCount = parentCount;
        }

        /// <summary>
        /// The field behind the Template property.
        /// </summary>
        internal Template Template;

        /// <summary>
        /// Employed by the template code generator.
        /// </summary>
        /// <value>The global namespace.</value>
        internal string GlobalNamespace
        {
            get
            {
                Template current = Template;
                while (current.Parent != null) current = (Template)current.Parent;
                return ((AppTemplate)current).Namespace;
            }
        }
    }
}
