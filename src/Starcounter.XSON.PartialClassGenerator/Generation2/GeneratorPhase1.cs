using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Creates a code dom from a JSON template (TObject).
    /// </summary>
    internal class GeneratorPhase1 {
        private Gen2DomGenerator Generator;

        internal GeneratorPhase1(Gen2DomGenerator generator) {
            this.Generator = generator;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template (TObject).
        /// </summary>
        /// <param name="at">The TJson template (i.e. json tree prototype) to generate code for</param>
        /// <param name="metadata">The metadata.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        internal AstRoot RunPhase1(TObject at, out AstJsonClass acn, out AstSchemaClass tcn, out AstMetadataClass mcn) {
            CodeBehindMetadata metadata = Generator.CodeBehindMetadata;

            var root = new AstRoot(Generator);
            acn = (AstJsonClass)Generator.ObtainValueClass(at);
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
        private void GenerateKids(AstInstanceClass appClassParent,
                                  AstTemplateClass templParent,
                                  AstMetadataClass metaParent,
                                  Template template) {
            if (template is TContainer) {
                var pt = (TContainer)template;
                foreach (var kid in pt.Children) {
                    if (kid is TContainer) {
                        if (kid is TObject) {
                            GenerateForApp((TObject)kid,
                                           appClassParent,
                                           templParent,
                                           metaParent,
                                           template);
                        }
                        else if (kid is TObjArr) {
							var tarr = kid as TObjArr;
                            var titem = tarr.ElementType;
							var isUntyped = ((titem == null) || (titem.Properties.Count == 0));

                            if (isUntyped) {
                                if (titem != null && titem.GetCodegenMetadata(Gen2DomGenerator.Reuse) != null)
                                    GenerateClassesForReusedJson(tarr, titem.GetCodegenMetadata(Gen2DomGenerator.Reuse));
                                else 
								    GenerateClassesForDefaultArray(tarr);
                            }

							GenerateProperty(
								kid,
								(AstJsonClass)appClassParent,
								(AstSchemaClass)templParent,
								metaParent);
							
							if (!isUntyped){
								GenerateForApp(((TObjArr)kid).ElementType,
											   appClassParent,
											   templParent,
											   metaParent,
											   template);
							}
                        }
                        else {
                            throw new Exception();
                        }
                    }
                    else {
                        AstTemplateClass type = Generator.ObtainTemplateClass(kid);
                        Generator.TemplateClasses[kid] = type;

                        GenerateProperty(
                            kid, 
                            (AstJsonClass)appClassParent, 
                            (AstSchemaClass)templParent, 
                            metaParent);
                    }
                }
            }
        }

        private void GenerateClassesForReusedJson(TObjArr template, string reuseTypeName) {
            var acn = Generator.GetJsonArrayClass(reuseTypeName);

            Generator.ValueClasses[template] = acn;
            Generator.TemplateClasses[template] = acn.NTemplateClass;
            Generator.MetaClasses[template] = acn.NMetadataClass;
        }

        private void GenerateClassesForReusedJson(TObject template, string reuseTypeName) {
            var acn = Generator.GetJsonClass(reuseTypeName, template);

            Generator.ValueClasses[template] = acn;
            Generator.TemplateClasses[template] = acn.NTemplateClass;
            Generator.MetaClasses[template] = acn.NMetadataClass;
        }

		private void GenerateClassesForDefaultArray(TObjArr template) {
			var acn = Generator.GetDefaultJsonArrayClass();
			template.ElementType = (TObject)acn.NTemplateClass.Template;

			Generator.ValueClasses[template] = acn;
			Generator.TemplateClasses[template] = acn.NTemplateClass;
			Generator.MetaClasses[template] = acn.NMetadataClass;
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
            var valueClass = Generator.ObtainValueClass(at);
            var type = Generator.ObtainTemplateClass(at);

            new AstProperty(Generator) {
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
                Type = type
            };
            new AstProperty(Generator) {
                Parent = metaParent,
                Template = at,
                Type = Generator.ObtainMetaClass(at)
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
        private void GenerateForApp(TObject at,
                                    AstInstanceClass appClassParent,
                                    AstTemplateClass templParent,
                                    AstMetadataClass metaParent,
                                    Template template) {
            AstJsonClass acn;
            AstTemplateClass tcn;
            AstMetadataClass mcn;

            // Untyped or typed.
            // Parent template is array or object


            if (at.Properties.Count == 0) {
                string reuse = at.GetCodegenMetadata(Gen2DomGenerator.Reuse);

                //if (reuse != null) {
                //    acn = GenerateClassesForReusedJson(at, reuse);
                //    Generator.ObtainValueClass
                    
                //} else {
                    // Empty App templates does not typically receive a custom template 
                    // class (unless explicitly set by the Json.nnnn syntax (TODO)
                    // This means that they can be assigned to any App object. 
                    // A typical example is to have a Page:{} property in a master
                    // app (representing, for example, child web pages)
                    acn = (AstJsonClass)Generator.GetDefaultJson();
                    //                tcn = Generator.TemplateClasses[Generator.DefaultObjTemplate];
                    //                mcn = Generator.MetaClasses[Generator.DefaultObjTemplate];
                //}
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
            }
            else {
                AstJsonClass racn;
                acn = racn = (AstJsonClass)Generator.ObtainValueClass(at);
                acn.Parent = appClassParent;
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
                GenerateKids( acn, tcn, mcn, at );
            }
            Generator.ValueClasses[at] = acn;
            Generator.TemplateClasses[at] = tcn;
            Generator.MetaClasses[at] = mcn;

            if (at.Parent is TObject)
                GenerateProperty(
                    at, 
                    (AstJsonClass)appClassParent, 
                    (AstSchemaClass)templParent, 
                    metaParent);
        }
    }
}
