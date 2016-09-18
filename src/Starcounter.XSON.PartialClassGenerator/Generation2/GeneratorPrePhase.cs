using System;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
    /// </summary>
    internal class GeneratorPrePhase {
        private Gen2DomGenerator generator;

        internal GeneratorPrePhase(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPrePhase(TValue prototype) {
            ProcessInstanceTypeAssignments(prototype, generator.CodeBehindMetadata);
        }

        /// <summary>
        /// Processes all typeassignments of 'InstanceType' that are in the metadata and makes sure 
        /// that the specified conversions and reuses are valid as well as do the actual conversion.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="metadata"></param>
        private void ProcessInstanceTypeAssignments(TValue prototype, CodeBehindMetadata metadata) {
            foreach (var classInfo in metadata.CodeBehindClasses) {
                TValue classRoot = generator.FindTemplate(classInfo, prototype);
                foreach (var typeAssignment in classInfo.InstanceTypeAssignments) {
                    ProcessOneTypeAssignment(classRoot, typeAssignment);
                }
            }
        }
        
        private void ProcessOneTypeAssignment(TValue root, CodeBehindTypeAssignmentInfo typeAssignment) {
            string[] parts = typeAssignment.TemplatePath.Split('.');
            Template theTemplate;
            TObject currentObject;

            if (!"DefaultTemplate".Equals(parts?[0])) {
                generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                     "Path does not start with field 'DefaultTemplate'",
                                                     null,
                                                     root.CodegenInfo.SourceInfo);
            }

            if (parts.Length == 1) {
                root.CodegenInfo.ReuseType = typeAssignment.TypeName;
            } else {
                currentObject = root as TObject;
                if (currentObject == null) {
                    generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                     "Member '" + root.PropertyName + "' is not an object",
                                                     null,
                                                     root.CodegenInfo.SourceInfo);
                }

                for (int i = 1; i < parts.Length - 1; i++) {
                    currentObject = currentObject.Properties.GetTemplateByPropertyName(parts[i]) as TObject;
                    if (currentObject == null) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                         "Member '" + parts[i] + "' was not found",
                                                         null,
                                                         root.CodegenInfo.SourceInfo);
                    }
                }

                theTemplate = currentObject.Properties.GetTemplateByPropertyName(parts[parts.Length - 1]);
                if (theTemplate == null) {
                    generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                     "Member '" + parts[parts.Length - 1] + "' was not found",
                                                     null,
                                                     root.CodegenInfo.SourceInfo);
                }

                Template newTemplate = CheckAndProcessTypeConversion(theTemplate, typeAssignment.TypeName);
                if (newTemplate != null) {
                    currentObject.Properties.Replace(newTemplate);
                    newTemplate.BasedOn = null;
                }
            }
        }

        /// <summary>
        /// Checks that a conversion is possible between the type specified and the type of the 
        /// template and process the actual conversion. 
        /// If the template is an object or array, the type will be stored on the template 
        /// directly and processed later. Otherwise, if the conversion is valid, a new template 
        /// will be created.
        /// </summary>
        /// <param name="template">The original template</param>
        /// <param name="typeName">The type of the instance of the template to convert to.</param>
        /// <returns>
        /// A new template if the conversion is for a primitive value, otherwise 
        /// null (even if the conversion was succesful)
        /// </returns>
        private Template CheckAndProcessTypeConversion(Template template, string typeName) {
            Template newTemplate = null;
            
            switch (template.TemplateTypeId) {
                case TemplateTypeEnum.Decimal:
                    if (IsDoubleType(typeName)) {
                        newTemplate = new TDouble();
                        template.CopyTo(newTemplate);
                        ((TDouble)newTemplate).DefaultValue = Convert.ToDouble(((TDecimal)template).DefaultValue);
                    } else if (!IsDecimalType(typeName)) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             null,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    break;
                case TemplateTypeEnum.Double:
                    if (IsDecimalType(typeName)) {
                        newTemplate = new TDecimal();
                        template.CopyTo(newTemplate);
                        ((TDecimal)newTemplate).DefaultValue = Convert.ToDecimal(((TDouble)template).DefaultValue);
                    } else if (!IsDoubleType(typeName)) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             null,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    break;
                case TemplateTypeEnum.Object:
                    if (((TObject)template).Properties.Count > 0) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             null,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    template.CodegenInfo.ReuseType = typeName;
                    break;

                // Currently reusing type on array is not implemented properly, so for now we block 
                // the possiblity to have an error when trying to set instancetype.
                //case TemplateTypeEnum.Array:
                //    var elementTemplate = ((TObjArr)template).ElementType;
                //    if (elementTemplate != null) {
                //        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEREUSE,
                //                                             null,
                //                                             null,
                //                                             template.CodegenInfo.SourceInfo);
                //    }
                //    template.CodegenInfo.ReuseType = typeName;
                //    break;
                default:
                    generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             null,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    break;
            }
            return newTemplate;
        }

        private bool IsDoubleType(string typeName) {
            return (typeName.Equals("double", StringComparison.InvariantCultureIgnoreCase)
                        || typeName.Equals("System.Double"));
        }

        private bool IsDecimalType(string typeName) {
            return (typeName.Equals("decimal", StringComparison.InvariantCultureIgnoreCase)
                        || typeName.Equals("System.Decimal"));
        }
    }
}
