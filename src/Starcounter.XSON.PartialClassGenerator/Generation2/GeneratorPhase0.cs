using System;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    internal class GeneratorPhase0 {
        private Gen2DomGenerator generator;

        internal GeneratorPhase0(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase0(TValue prototype) {
            ProcessInstanceTypeAssignments(prototype, generator.CodeBehindMetadata);
        }

        private void ProcessInstanceTypeAssignments(TValue prototype, CodeBehindMetadata metadata) {
            foreach (var classInfo in metadata.CodeBehindClasses) {
                TValue classRoot = generator.FindTemplate(classInfo, prototype);
                if (classRoot == null)
                    throw new Exception("TODO 0");

                foreach (var typeAssignment in classInfo.InstanceTypeAssignments) {
                    ProcessOneTypeAssignment(classRoot, typeAssignment);
                }
            }
        }

        private void ProcessOneTypeAssignment(TValue root, CodeBehindTypeAssignmentInfo typeAssignment) {
            if (string.IsNullOrEmpty(typeAssignment.TemplatePath)) {
                root.CodegenInfo.ReuseType = typeAssignment.TypeName;
            } else {
                TObject tobj = root as TObject;
                if (tobj == null)
                    throw new Exception("Error 1");

                string[] parts = typeAssignment.TemplatePath.Split('.');
                
                for (int i = 0; i < parts.Length - 1; i++) {
                    tobj = tobj.Properties.GetTemplateByPropertyName(parts[i]) as TObject;
                    if (tobj == null)
                        throw new Exception("Error 2");
                }

                var theTemplate = tobj.Properties.GetTemplateByPropertyName(parts[parts.Length - 1]);
                if (theTemplate == null)
                    throw new Exception("Error 3");

                Template newTemplate = CheckValidTypeConversion(theTemplate, typeAssignment.TypeName);
                if (newTemplate != null) {
                    tobj.Properties.Replace(newTemplate);
                    newTemplate.BasedOn = null;
                }
            }
        }

        /// <summary>
        /// Checks that a conversion is possible between the type specified and the type of the template.
        /// If the template is an object
        /// </summary>
        /// <param name="template"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        private Template CheckValidTypeConversion(Template template, string typeName) {
            Template newTemplate = null;
            
            switch (template.TemplateTypeId) {
                case TemplateTypeEnum.Decimal:
                    if ("double".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) {
                        newTemplate = new TDouble();
                        template.CopyTo(newTemplate);
                        ((TDouble)newTemplate).DefaultValue = Convert.ToDouble(((TDecimal)template).DefaultValue);
                    } else if ("decimal".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) {
                        // Do nothing.
                    } else {
                        throw new Exception("Error 5");
                    }
                    break;
                case TemplateTypeEnum.Double:
                    if ("decimal".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) {
                        newTemplate = new TDecimal();
                        template.CopyTo(newTemplate);
                        ((TDecimal)newTemplate).DefaultValue = Convert.ToDecimal(((TDouble)template).DefaultValue);
                    } else if ("decimal".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)) {
                        // Do nothing.
                    } else {
                        throw new Exception("Error 6");
                    }
                    break;
                case TemplateTypeEnum.Object:
                    if (((TObject)template).Properties.Count > 0)
                        throw new Exception("Error 7");

                    template.CodegenInfo.ReuseType = typeName;
                    break;
                default:
                    throw new Exception("Error 8");
            }
            return newTemplate;
        }
    }
}
