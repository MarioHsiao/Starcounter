


using Starcounter.Templates;
using Starcounter.XSON.Metadata;
using System;
using TJson = Starcounter.Templates.TObject;


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
        public AstRoot RunPhase1(TJson at, out AstJsonClass acn, out AstSchemaClass tcn, out AstMetadataClass mcn) {

            CodeBehindMetadata metadata = Generator.CodeBehindMetadata;

            var root = new AstRoot(Generator);
            acn = (AstJsonClass)Generator.ObtainValueClass(at);
            acn.Parent = root;
            acn.IsPartial = true;
            acn.CodebehindClass = metadata.RootClassInfo;


 //           tcn = (AstSchemaClass)Generator.ObtainTemplateClass(at);


            if (metadata == CodeBehindMetadata.Empty) {
                // No codebehind. Need to set a few extra properties depending on metadata from json
                acn.IsPartial = false;
//                acn.InheritedClass = (AstJsonClass)Generator.ObtainValueClass(Generator.DefaultObjTemplate);

//                // A specific IBindable type have been specified in the json.
//                // We add it as a generic value on the Json-class this class inherits from.
 //               if (at.InstanceDataTypeName != null) {
 //                   //acn._Inherits += '<' + at.InstanceDataTypeName + '>';
 //                   tcn.AutoBindProperties = true;
 //               }
            }
//            else if (at.InstanceDataTypeName != null) {
//                Generator.ThrowExceptionWithLineInfo(Error.SCERRDUPLICATEDATATYPEJSON, "", null, at.CompilerOrigin);
//            }

            //mcn = Generator.ObtainMetaClass(at);
            //new AstObjMetadata(Generator) {
            //mcn.Parent = jsonbyexample;
            //mcn.NValueClass = acn;
            // mcn.InheritedClass = Generator.MetaClasses[Generator.DefaultObjTemplate];

            //tcn.NValueClass.NMetadataClass = mcn;

            // Generator.ValueClasses.Add(at, acn);
            //Generator.TemplateClasses.Add(at, tcn);
            //Generator.MetaClasses.Add(at, mcn);

            mcn = acn.NMetadataClass;
            tcn = (AstSchemaClass)acn.NTemplateClass;

#if DEBUG
            if (tcn.NValueClass != acn)
                throw new Exception();
            if (tcn.Template != at)
                throw new Exception();
//            if (tcn.Generic[0] != acn)
//                throw new Exception();
            if (tcn.NValueClass != acn)
                throw new Exception();

#endif


            root.AppClassClassNode = acn;
            //acn.NTemplateClass = tcn;
            GenerateKids(acn,
                        (AstSchemaClass)acn.NTemplateClass,
                        acn.NMetadataClass,
                        acn.NTemplateClass.Template);

//            root.RootJsonClassAlias = acn.GlobalClassSpecifier;
//            root.RootJsonClassAliasPrefix = acn.GlobalClassSpecifier + ".";

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
        private void GenerateKids(AstInstanceClass appClassParent,
                                  AstTemplateClass templParent,
                                  AstMetadataClass metaParent,
                                  Template template) {
            if (template is TContainer) {
                var pt = (TContainer)template;
                foreach (var kid in pt.Children) {
                    if (kid is TContainer) {
                        if (kid is TJson) {
                            GenerateForApp( (TJson)kid,
                                           appClassParent,
                                           templParent,
                                           metaParent,
                                           template);
                        }
                        else if (kid is TObjArr) {
							var tarr = kid as TObjArr;
							var isUntyped = ((tarr.ElementType == null) || (tarr.ElementType.Properties.Count == 0));

                            if (isUntyped) {
                                if (tarr.elementTypeName != null)
                                    GenerateClassesForReusedJson(tarr);
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

                        
                        // Orphaned by design as primitive types dont get custom template classes
//                        var type = new AstPropertyClass(Generator) {
//                            Template = kid,
//                            // Parent = appClassParent,
//                            Namespace = "orphaned"
//                        };

                        var type = Generator.ObtainTemplateClass(kid);



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

        private void GenerateClassesForReusedJson(TObjArr template) {
            var acn = Generator.GetJsonArrayClass(template.elementTypeName);
//            template.ElementType = (TJson)acn.NTemplateClass.Template;

            Generator.ValueClasses[template] = acn;
            Generator.TemplateClasses[template] = acn.NTemplateClass;
            Generator.MetaClasses[template] = acn.NMetadataClass;
        }

		private void GenerateClassesForDefaultArray(TObjArr template) {
			var acn = Generator.GetDefaultJsonArrayClass();
			template.ElementType = (TJson)acn.NTemplateClass.Template;

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
            var valueClass = Generator.ObtainValueClass(at );
            //valueClass.Parent = appClassParent;
            var type = Generator.ObtainTemplateClass(at );

            //type.NValueProperty = 
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
        private void GenerateForApp(TJson at,
                                    AstInstanceClass appClassParent,
                                    AstTemplateClass templParent,
                                    AstMetadataClass metaParent,
                                    Template template) {
            AstJsonClass acn;
            AstTemplateClass tcn;
            AstMetadataClass mcn;
            if (at.Properties.Count == 0) {
                // Empty App templates does not typically receive a custom template 
                // class (unless explicitly set by the Json.nnnn syntax (TODO)
                // This means that they can be assigned to any App object. 
                // A typical example is to have a Page:{} property in a master
                // app (representing, for example, child web pages)
                acn = (AstJsonClass)Generator.GetDefaultJson();
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
//                tcn = Generator.TemplateClasses[Generator.DefaultObjTemplate];
//                mcn = Generator.MetaClasses[Generator.DefaultObjTemplate];
            }
            else {
                AstJsonClass racn;
                acn = racn = (AstJsonClass)Generator.ObtainValueClass(at);
                acn.Parent = appClassParent;
                    //_Inherits = "global::" + Generator.DefaultObjTemplate.InstanceType.FullName // "Puppet", "Json"
//                acn.InheritedClass = (AstJsonClass)Generator.ValueClasses[Generator.DefaultObjTemplate];

//                tcn = new AstSchemaClass(Generator) {
//                    Parent = racn,
//                    Template = at,
//                    NValueClass = racn,
//                    ` Generator.TemplateClasses[Generator.DefaultObjTemplate],
//                    Generic = new AstClass[] { acn }
//                };

//                // A specific IBindable type have been specified in the json.
//                // We add it as a generic value on the Json-class this class inherits from.
//                if (at.InstanceDataTypeName != null) {
////                    racn._Inherits += '<' + at.InstanceDataTypeName + '>';
//                    ((AstTAppClass)tcn).AutoBindProperties = true;
//                }
//                tcn = Generator.ObtainTemplateClass(at);

//                mcn = Generator.ObtainMetaClass(at); //new AstObjMetadata(Generator);
//                mcn.Parent = racn;
                //mcn.NTemplateClass = tcn;
//                mcn.NValueClass = acn;
//                mcn.InheritedClass = Generator.MetaClasses[((AstJsonClass)acn.InheritedClass).NTemplateClass.Template];
//                racn.NMetadataClass = mcn;
//                racn.NTemplateClass = tcn;
                tcn = acn.NTemplateClass;
                mcn = acn.NMetadataClass;
                GenerateKids( acn, tcn, mcn, at );

//                if (!appClassParent.Children.Remove(acn))
//                    throw new Exception(); // Move to...
//                appClassParent.Children.Add(acn); // Move to...
//                if (!acn.Children.Remove(tcn))
//                    throw new Exception(); // Move to...
//                acn.Children.Add(tcn); // Move to...
//                if (!acn.Children.Remove(mcn))
//                    throw new Exception(); // ...last member
//                acn.Children.Add(mcn); // ...last member


            }
            Generator.ValueClasses[at] = acn;
            Generator.TemplateClasses[at] = tcn;
            Generator.MetaClasses[at] = mcn;

            if (at.Parent is TJson)
                GenerateProperty(
                    at, 
                    (AstJsonClass)appClassParent, 
                    (AstSchemaClass)templParent, 
                    metaParent);


        }

        
    }
}
