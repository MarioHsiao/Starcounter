using System;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Creates a code dom from a JSON template. 
    /// </summary>
    internal class GeneratorPhase1 {
        // TODO:
        // The implementation of the constructor is a bit flawed. The behaviour of this AST node is
        // not consistent with how the rest works. Also, the AST constructor is only used for the 
        // schema-class and not for the json-class, even though we generate code for constructors in
        // json.
        // This will mean that in the codegenerator for C#, the handling of the constructor is somewhat
        // hardcoded.

        private Gen2DomGenerator generator;

        internal GeneratorPhase1(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        /// <summary>
        /// This is the main calling point to generate a dom tree for a JSON template.
        /// </summary>
        /// <param name="prototype">The json template (i.e. json tree prototype) to generate code for</param>
        /// <param name="mainJsonClass">The AST node for the main jsonclass.</param>
        /// <param name="mainSchemaClass">The AST node for the main schema (template) class.</param>
        /// <param name="mainMetadataClass">The AST node for the main metadata class.</param>
        /// <returns>An abstract code tree. Use CSharpGenerator to generate .CS code.</returns>
        internal AstRoot RunPhase1(TValue prototype, 
                                   out AstJsonClass mainJsonClass, 
                                   out AstSchemaClass mainSchemaClass, 
                                   out AstMetadataClass mainMetadataClass) {
            CodeBehindMetadata metadata = generator.CodeBehindMetadata;

            var root = new AstRoot(generator);
            mainJsonClass = generator.ObtainRootValueClass(prototype);
            mainJsonClass.Parent = root;
            mainJsonClass.IsPartial = true;
            mainJsonClass.CodebehindClass = metadata.RootClassInfo;

            if (metadata == CodeBehindMetadata.Empty) {
                // No codebehind. Need to set a few extra properties depending on metadata from json
                mainJsonClass.IsPartial = false;
            }

            mainMetadataClass = mainJsonClass.NMetadataClass;
            mainSchemaClass = (AstSchemaClass)mainJsonClass.NTemplateClass;

            root.AppClassClassNode = mainJsonClass;

            GenerateChildren(mainJsonClass.NTemplateClass.Template,
                         mainJsonClass,
                         (AstSchemaClass)mainJsonClass.NTemplateClass,
                         mainJsonClass.NMetadataClass);

            return root;
        }

        /// <summary>
        /// Generates nodes for all children of the specified template.
        /// </summary>
        /// <param name="prototype">The (prototype) template to generate kids for.</param>
        /// <param name="jsonParent">The AST node that is the parent json.</param>
        /// <param name="schemaParent">The AST node that is the parent schema class.</param>
        /// <param name="metadataParent">The AST node that is the parent metadata class.</param>
        /// <exception cref="System.Exception"></exception>
        private void GenerateChildren(Template prototype,
                                      AstInstanceClass jsonParent, 
                                      AstTemplateClass schemaParent, 
                                      AstMetadataClass metadataParent
                                  ) {
            if (prototype is TObject) {
                foreach (var childPrototype in ((TObject)prototype).Children) {
                    switch (childPrototype.TemplateTypeId) {
                        case TemplateTypeEnum.Object:
                            GenerateClass((TObject)childPrototype, jsonParent, schemaParent, metadataParent);
                            break;
                        case TemplateTypeEnum.Array:
                            GenerateArray((TObjArr)childPrototype, jsonParent, schemaParent, metadataParent, true);
                            break;
                        default:
                            // TODO: Why do we obtain the type here?
                            AstTemplateClass type = generator.ObtainTemplateClass(childPrototype);
                            GenerateProperty(childPrototype, (AstJsonClass)jsonParent, (AstSchemaClass)schemaParent, metadataParent);
                            break;
                    }
                }
            } else if (prototype is TObjArr) {
                GenerateArray((TObjArr)prototype, jsonParent, schemaParent, metadataParent, false);
            } else {
                // TODO:
                // Why do we NOT obtain a type here like we do above?
                GenerateProperty(prototype, 
                                (AstJsonClass)jsonParent, 
                                (AstSchemaClass)schemaParent, 
                                metadataParent, 
                                GetPropertyNameForSinglePrimitiveTemplate(jsonParent.NTemplateClass.Template));
            }
        }

        /// <summary>
        /// Generates nodes for all children of the specified object.
        /// </summary>
        /// <param name="prototype">The (prototype) template to generate kids for.</param>
        /// <param name="jsonParent">The AST node that is the parent json.</param>
        /// <param name="schemaParent">The AST node that is the parent schema class.</param>
        /// <param name="metadataParent">The AST node that is the parent metadata class.</param>
        /// <param name="generateProperty">If true, a property will be generated as well.</param>
        /// <exception cref="System.Exception"></exception>
        private void GenerateArray(TObjArr prototype,
                                   AstInstanceClass jsonParent,
                                   AstTemplateClass schemaParent,
                                   AstMetadataClass metadataParent,
                                   bool generateProperty) {
            TObject objElement = null;
            var tarr = prototype as TObjArr;
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
            
            if (generateProperty) {
                GenerateProperty(prototype, (AstJsonClass)jsonParent, (AstSchemaClass)schemaParent, metadataParent);
            }
            
            if (!isUntyped && titem != null) {
                GenerateClass(titem,
                              jsonParent,
                              schemaParent,
                              metadataParent);
            }
        }
        
        /// <summary>
        /// Add AST nodes that describes a property in the schema for the specified template.
        /// </summary>
        /// <param name="template">The template that the property should be based on.</param>
        /// <param name="jsonParent">The Json class the property should be added to.</param>
        /// <param name="schemaParent">The schema class the property should be added to.</param>
        /// <param name="metadataParent">The metadata class the property should be added to.</param>
        /// <param name="propertyNameOverride">
        /// If specified, this value will be used as membername instead of using the name from the template.
        /// </param>
        private void GenerateProperty(Template template,
                                      AstJsonClass jsonParent,
                                      AstSchemaClass schemaParent,
                                      AstClass metadataParent,
                                      string propertyNameOverride = null)
        {
            var valueClass = generator.ObtainValueClass(template);
            var type = generator.ObtainTemplateClass(template);

            // Node for accessor of the value in the jsonclass (including backingfield).
            new AstProperty(generator) {
                Parent = jsonParent,
                Template = template,
                Type = valueClass,
                MemberName = propertyNameOverride
            };

            // Node for accessor of the template in the schema.
            new AstProperty(generator) {
                Parent = schemaParent,
                Template = template,
                Type = type,
                MemberName = propertyNameOverride
            };

            // Node for entry in the constructor of the schema to initalize the template.
            new AstProperty(generator) {
                Parent = schemaParent.Constructor,
                Template = template,
                Type = type,
                MemberName = propertyNameOverride
            };

            // Node for accessor of the template in metadata.
            new AstProperty(generator) {
                Parent = metadataParent,
                Template = template,
                Type = generator.ObtainMetaClass(template),
                MemberName = propertyNameOverride
            };
        }

        /// <summary>
        /// Generates the ast nodes for a jsonclass and the corresponding
        /// schema and metadata classes.
        /// </summary>
        /// <param name="template">The template to generate a custom class for.</param>
        /// <param name="jsonParent"></param>
        /// <param name="schemaParent"></param>
        /// <param name="metadataParent"></param>
        private void GenerateClass(TValue template,
                                   AstInstanceClass jsonParent,
                                   AstTemplateClass schemaParent,
                                   AstMetadataClass metadataParent) {
            AstJsonClass jsonClass;
            AstTemplateClass schemaClass;
            AstMetadataClass metadataClass;

            // Untyped or typed.
            // Parent template is array or object
            TObject tobj = template as TObject;
            if (tobj != null && tobj.Properties.Count == 0) {
                string reuse = template.GetCodegenMetadata(Gen2DomGenerator.Reuse);

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
                jsonClass = (AstJsonClass)generator.ObtainRootValueClass(tobj);
                schemaClass = jsonClass.NTemplateClass;
                metadataClass = jsonClass.NMetadataClass;
            } else {
                jsonClass = (AstJsonClass)generator.ObtainRootValueClass(template);
                jsonClass.Parent = jsonParent;
                schemaClass = jsonClass.NTemplateClass;
                metadataClass = jsonClass.NMetadataClass;

                GenerateChildren(template, jsonClass, schemaClass, metadataClass);
            }
            
            if (template.Parent is TObject) {
                GenerateProperty(
                    template,
                    (AstJsonClass)jsonParent,
                    (AstSchemaClass)schemaParent,
                    metadataParent);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private string GetPropertyNameForSinglePrimitiveTemplate(Template template) {
            var enumValue = (TemplateTypeEnum)template.TemplateTypeId;
            switch (enumValue) {
                case TemplateTypeEnum.Bool:
                    return "BoolValue";
                case TemplateTypeEnum.Decimal:
                    return "DecimalValue";
                case TemplateTypeEnum.Double:
                    return "DoubleValue";
                case TemplateTypeEnum.Long:
                    return "IntegerValue";
                case TemplateTypeEnum.String:
                    return "StringValue";
            }
            throw new Exception("Unknown templatetype: " + template.GetType());
        }
    }
}
