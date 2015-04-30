using System;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Creates a code dom from a JSON template (TObject). 
    /// </summary>
    internal class GeneratorPhase1 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase1(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template.
        /// </summary>
        /// <param name="at">The json template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        internal AstRoot RunPhase1(TValue at, out AstJsonClass acn, out AstSchemaClass tcn, out AstMetadataClass mcn) {
            CodeBehindMetadata metadata = generator.CodeBehindMetadata;

            var root = new AstRoot(generator);
            acn = generator.ObtainRootValueClass(at);
            acn.Parent = root;
            acn.IsPartial = true;
            acn.CodebehindClass = metadata.RootClassInfo;

            if (metadata == CodeBehindMetadata.Empty) {
                // No codebehind. Need to set a few extra properties depending on metadata from json
                acn.IsPartial = false;
            }

            mcn = acn.NMetadataClass;
            tcn = (AstSchemaClass)acn.NTemplateClass;

#if DEBUG
            if (tcn.NValueClass != acn)
                throw new Exception();
            if (tcn.Template != at)
                throw new Exception();
            if (tcn.NValueClass != acn)
                throw new Exception();

#endif

            root.AppClassClassNode = acn;
            GenerateKids(acn,
                        (AstSchemaClass)acn.NTemplateClass,
                        acn.NMetadataClass,
                        acn.NTemplateClass.Template);

            return root;
        }

        /// <summary>
        /// Generates the kids.
        /// </summary>
        /// <param name="appClassParent">The app class parent.</param>
        /// <param name="templParent">The templ parent.</param>
        /// <param name="metaParent">The meta parent.</param>
        /// <param name="template">The template.</param>
        /// <exception cref="System.Exception"></exception>
        private void GenerateKids(AstInstanceClass appClassParent, AstTemplateClass templParent, AstMetadataClass metaParent, Template template) {
            if (template is TObject) {
                var pt = (TContainer)template;
                foreach (var kid in pt.Children) {
                    if (kid is TContainer) {
                        if (kid is TObject) {
                            GenerateForApp((TObject)kid,
                                           appClassParent,
                                           templParent,
                                           metaParent,
                                           template);
                        } else if (kid is TObjArr) {
                            TObject objElement = null;
                            var tarr = kid as TObjArr;
                            var titem = tarr.ElementType;

                            // untyped if titem == null or titem is an object with no properties.
                            bool isUntyped = (titem == null);
                            if (!isUntyped) {
                                objElement = titem as TObject;
                                if (objElement != null)
                                    isUntyped = (objElement.Properties.Count == 0);
                            }

                            if (isUntyped) {
                                if (titem != null && titem.GetCodegenMetadata(Gen2DomGenerator.Reuse) != null)
                                    generator.AssociateTemplateWithReusedArray(tarr, titem.GetCodegenMetadata(Gen2DomGenerator.Reuse));
                                else {
                                    generator.AssociateTemplateWithDefaultArray(tarr);
                                }
                            }

                            GenerateProperty(
                                kid,
                                (AstJsonClass)appClassParent,
                                (AstSchemaClass)templParent,
                                metaParent);

                            // TODO:
                            // Need to generate class for primitive template as well I think.
                            if (!isUntyped && objElement != null) {
                                GenerateForApp(objElement,
                                               appClassParent,
                                               templParent,
                                               metaParent,
                                               template);
                            }
                        } else {
                            throw new Exception();
                        }
                    } else {
                        AstTemplateClass type = generator.ObtainTemplateClass(kid);
                        GenerateProperty(
                            kid,
                            (AstJsonClass)appClassParent,
                            (AstSchemaClass)templParent,
                            metaParent);
                    }
                }
            } else if (template is TObjArr) {
                TObject objElement = null;
                var tarr = template as TObjArr;
                var titem = tarr.ElementType;

                // untyped if titem == null or titem is an object with no properties.
                bool isUntyped = (titem == null);
                if (!isUntyped) {
                    objElement = titem as TObject;
                    if (objElement != null)
                        isUntyped = (objElement.Properties.Count == 0);
                }

                if (isUntyped) {
                    if (titem != null && titem.GetCodegenMetadata(Gen2DomGenerator.Reuse) != null)
                        generator.AssociateTemplateWithReusedArray(tarr, titem.GetCodegenMetadata(Gen2DomGenerator.Reuse));
                    else {
                        generator.AssociateTemplateWithDefaultArray(tarr);
                    }
                    titem = null;
                }

                //GenerateProperty(
                //    kid,
                //    (AstJsonClass)appClassParent,
                //    (AstSchemaClass)templParent,
                //    metaParent);

                // TODO:
                // Need to generate class for primitive template as well I think.
                if (!isUntyped && titem != null) {
                    GenerateForApp(titem,
                                   appClassParent,
                                   templParent,
                                   metaParent,
                                   template);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="at"></param>
        /// <param name="appClassParent"></param>
        /// <param name="templParent"></param>
        /// <param name="metaParent"></param>
        private void GenerateProperty(Template at,
                                      AstJsonClass appClassParent,
                                      AstSchemaClass templParent,
                                      AstClass metaParent)
        {
            var valueClass = generator.ObtainValueClass(at);
            var type = generator.ObtainTemplateClass(at);

            new AstProperty(generator) {
                Parent = appClassParent,
                Template = at,
                Type = valueClass
            };
            new AstProperty(generator) {
                Parent = templParent,
                Template = at,
                Type = type
            };
            new AstProperty(generator) {
                Parent = templParent.Constructor,
                Template = at,
                Type = type
            };
            new AstProperty(generator) {
                Parent = metaParent,
                Template = at,
                Type = generator.ObtainMetaClass(at)
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
        private void GenerateForApp(TValue at,
                                    AstInstanceClass appClassParent,
                                    AstTemplateClass templParent,
                                    AstMetadataClass metaParent,
                                    Template template) {
            AstJsonClass acn;
            AstTemplateClass tcn;
            AstMetadataClass mcn;

            // Untyped or typed.
            // Parent template is array or object
            TObject tobj = at as TObject;
            if (tobj != null && tobj.Properties.Count == 0) {
                string reuse = at.GetCodegenMetadata(Gen2DomGenerator.Reuse);

                if (reuse != null) {
                    generator.AssociateTemplateWithReusedJson(tobj, reuse);
                } else {
                    // Empty App templates does not typically receive a custom template 
                    // class (unless explicitly set by the Json.nnnn syntax (TODO)
                    // This means that they can be assigned to any App object. 
                    // A typical example is to have a Page:{} property in a master
                    // app (representing, for example, child web pages)
                    generator.AssociateTemplateWithDefaultJson(tobj);
                }
                acn = (AstJsonClass)generator.ObtainRootValueClass(tobj);
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
            } else {
                acn = (AstJsonClass)generator.ObtainRootValueClass(at);
                acn.Parent = appClassParent;
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
                GenerateKids( acn, tcn, mcn, at );
            }

            if (at.Parent is TObject)
                GenerateProperty(
                    at, 
                    (AstJsonClass)appClassParent, 
                    (AstSchemaClass)templParent, 
                    metaParent);
        }
    }
}
