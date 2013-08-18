


using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Creates a code dom from a JSON template (TJson).
    /// </summary>
    internal class GeneratorPhase1 {

        internal Gen2DomGenerator Generator;


        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TJson).
        /// </summary>
        /// <param name="at">The TJson template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        public AstRoot RunPhase1(TObj at, out AstAppClass acn, out AstTAppClass tcn, out AstObjMetadata mcn ) {

            CodeBehindMetadata metadata = Generator.CodeBehindMetadata;

            var root = new AstRoot(Generator);
            acn = new AstAppClass(Generator) {
                Parent = root,
                IsPartial = true
            };

            tcn = new AstTAppClass(Generator) {
                Parent = acn,
                NValueClass = acn,
                Template = at,
                _Inherits = Generator.DefaultObjTemplate.GetType().Name, // "TPuppet,TJson",
                AutoBindProperties = metadata.RootClassInfo.AutoBindToDataObject
            };

            if (metadata == CodeBehindMetadata.Empty) {
                // No codebehind. Need to set a few extra properties depending on metadata from json
                acn.IsPartial = false;
                acn._Inherits = Generator.DefaultObjTemplate.InstanceType.Name;

                // A specific IBindable type have been specified in the json.
                // We add it as a generic value on the Json-class this class inherits from.
                if (at.InstanceDataTypeName != null) {
                    acn._Inherits += '<' + at.InstanceDataTypeName + '>';
                    tcn.AutoBindProperties = true;
                }
            }
            else if (at.InstanceDataTypeName != null) {
                Generator.ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, at.CompilerOrigin);
            }

            mcn = new AstObjMetadata(Generator) {
                Parent = acn,
                NTemplateClass = tcn,
                _Inherits = "ObjMetadata"
            };

            tcn.NMetadataClass = mcn;

            Generator.ValueClasses[at] = acn;
            Generator.TemplateClasses[at] = tcn;
            Generator.MetaClasses[at] = mcn;

            root.AppClassClassNode = acn;
            acn.NTemplateClass = tcn;
            GenerateKids(acn,
                        (AstTAppClass)acn.NTemplateClass,
                        acn.NTemplateClass.NMetadataClass,
                        acn.NTemplateClass.Template);

            return root;
            //  TODOJOCKE                GenerateJsonAttributes(acn, json);

            //  TODOJOCKE              GenerateInputAttributes(acn);

            // TODOJOCKE                ConnectCodeBehindClasses(root, metadata);
            //  TODOJOCKE              GenerateInputBindings((AstTAppClass)acn.NTemplateClass, metadata);
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
                        }
                        else if (kid is TObjArr) {
                            // Orphaned by design as primitive types dont get custom template classes
                            //                            var type = new NArrXXXClass(NValueClass.Classes[kid.InstanceType] ) { Template = kid }; 
                            //                            NTemplateClass.Classes[kid] = type;

                            GenerateForArr(kid as TObjArr,
                                               appClassParent,
                                               templParent,
                                               metaParent,
                                               template);
                        }
                        else {
                            throw new Exception();
                        }
                    }
                    else {
                        // Orphaned by design as primitive types dont get custom template classes
                        var type = new AstPropertyClass(Generator) { Template = kid /*, Parent = appClassParent */ };
                        Generator.TemplateClasses[kid] = type;

                        GenerateProperty(kid, appClassParent, templParent, metaParent);
                    }
                }
            }
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
            var valueClass = Generator.FindValueClass(at);
            var type = Generator.FindTemplateClass(at);

            type.NValueProperty = new AstProperty(Generator) {
                Parent = appClassParent,
                Template = at,
                Type = valueClass
            };
            new AstProperty(Generator) {
                Parent = templParent,
                Template = at,
                Type = type
            };
            new AstProperty(Generator) {
                Parent = templParent.Constructor,
                Template = at,
                Type = Generator.FindTemplateClass(at)
            };
            new AstProperty(Generator) {
                Parent = metaParent,
                Template = at,
                Type = Generator.FindMetaClass(at)
            };
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
            AstAppClass acn;
            AstTemplateClass tcn;
            AstMetadataClass mcn;
            if (at.Properties.Count == 0) {
                // Empty App templates does not typically receive a custom template 
                // class (unless explicitly set by the Json.nnnn syntax (TODO)
                // This means that they can be assigned to any App object. 
                // A typical example is to have a Page:{} property in a master
                // app (representing, for example, child web pages)
                acn = (AstAppClass)Generator.ValueClasses[Generator.DefaultObjTemplate];
                tcn = Generator.TemplateClasses[Generator.DefaultObjTemplate];
                mcn = Generator.MetaClasses[Generator.DefaultObjTemplate];
            }
            else {
                AstAppClass racn;
                acn = racn = new AstAppClass(Generator) {
                    Parent = appClassParent,
                    _Inherits = Generator.DefaultObjTemplate.InstanceType.Name // "Puppet", "Json"
                };

                tcn = new AstTAppClass(Generator) {
                    Parent = racn,
                    Template = at,
                    NValueClass = racn,
                    _Inherits = Generator.DefaultObjTemplate.GetType().Name // "TPuppet", "TJson"
                };

                // A specific IBindable type have been specified in the json.
                // We add it as a generic value on the Json-class this class inherits from.
                if (at.InstanceDataTypeName != null) {
                    racn._Inherits += '<' + at.InstanceDataTypeName + '>';
                    ((AstTAppClass)tcn).AutoBindProperties = true;
                }

                mcn = new AstObjMetadata(Generator) {
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
            Generator.ValueClasses[at] = acn;
            Generator.TemplateClasses[at] = tcn;
            Generator.MetaClasses[at] = mcn;

            if (at.Parent is TObj)
                GenerateProperty(at, appClassParent, templParent, metaParent);


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
            var amn = new AstProperty(Generator) {
                Parent = appClassParent,
                Template = alt
            };
            var tmn = new AstProperty(Generator) {
                Parent = appClassParent.NTemplateClass,
                Template = alt
            };
            var cstmn = new AstProperty(Generator) {
                Parent = ((AstTAppClass)appClassParent.NTemplateClass).Constructor,
                Template = alt
            };
            var mmn = new AstProperty(Generator) {
                Parent = appClassParent.NTemplateClass.NMetadataClass,
                Template = alt
            };
            GenerateKids(appClassParent, templParent, metaParent, alt);
            var vlist = new AstArrXXXClass(Generator, "Arr", Generator.ValueClasses[alt.ElementType], null, alt);
            amn.Type = vlist;

            tmn.Type = new AstArrXXXClass(Generator, "TArr",
                                            Generator.ValueClasses[alt.ElementType],
                                            Generator.TemplateClasses[alt.ElementType], alt);
            cstmn.Type = new AstArrXXXClass(Generator, "TArr",
                                            Generator.ValueClasses[alt.ElementType],
                                            Generator.TemplateClasses[alt.ElementType], alt);

            mmn.Type = new AstArrXXXClass(Generator, "ArrMetadata",
                                            Generator.ValueClasses[alt.ElementType],
                                            Generator.TemplateClasses[alt.ElementType], alt);

            //ntempl.Template = alt;
            //            NTemplateClass.Classes[alt] = tlist;
            Generator.ValueClasses[alt] = vlist;
        }
    }
}
