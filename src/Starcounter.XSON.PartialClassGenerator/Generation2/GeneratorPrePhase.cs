using System;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Metadata;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
    /// </summary>
    internal class GeneratorPrePhase {
        private const string MEMBER_NOT_FOUND = "Member '{0}' in path '{1}' was not found.";
        private const string MEMBER_NOT_OBJ_OR_ARR = "Member '{0}' in path '{1}' is not an object or array.";
        private const string MEMBER_NOT_ELEMENTTYPE = "Expected member 'ElementType' but found '{0}' in path '{1}.";
        private const string VALID_TYPES_FLOAT = "Valid instancetype for this member is either 'decimal' or 'double'.";
        private const string VALID_TYPES_INT = "Valid instancetype for this member is 'decimal', 'double' or 'long'.";
        private const string ONLY_UNTYPED_OBJ = "Instancetype can only be assigned for objects without any members declared.";
        private const string INVALID_MEMBER_ASSIGNMENT = "Instancetype '{0}' cannot be assigned to member '{1}' in path '{2}'";

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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="typeAssignment"></param>
        private void ProcessOneTypeAssignment(TValue root, CodeBehindTypeAssignmentInfo typeAssignment) {
            string[] parts = typeAssignment.TemplatePath.Split('.');
            Template theTemplate;   
            Template currentTemplate;

            if (!"DefaultTemplate".Equals(parts?[0])) {
                generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                     "Path does not start with field 'DefaultTemplate'",
                                                     null,
                                                     root.CodegenInfo.SourceInfo);
            }

            if (parts.Length == 1) {
                root.CodegenInfo.ReuseType = typeAssignment.TypeName;
            } else {
                VerifyIsObjectOrArray(root.PropertyName, root, typeAssignment, root);

                currentTemplate = root;
                for (int i = 1; i < parts.Length - 1; i++) {
                    currentTemplate = GetChild(currentTemplate, parts[i], typeAssignment, root);
                    VerifyIsObjectOrArray(parts[i], currentTemplate, typeAssignment, root);
                }
                
                theTemplate = GetChild(currentTemplate, parts[parts.Length - 1], typeAssignment, root);
                if (theTemplate == null) {
                    generator.ThrowExceptionWithLineInfo(
                        Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                        string.Format(MEMBER_NOT_FOUND, parts[parts.Length - 1], typeAssignment.TemplatePath),
                        null,
                        root.CodegenInfo.SourceInfo
                    );
                }

                Template newTemplate = CheckAndProcessTypeConversion(theTemplate, typeAssignment);
                if (newTemplate != null) {
                    ((TObject)currentTemplate).Properties.Replace(newTemplate);
                    newTemplate.BasedOn = null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objOrArr"></param>
        /// <param name="name"></param>
        /// <param name="typeAssignment"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        private Template GetChild(Template objOrArr, 
                                  string name,
                                  CodeBehindTypeAssignmentInfo typeAssignment,
                                  Template root) {
            if (objOrArr.TemplateTypeId == TemplateTypeEnum.Object) {
               return ((TObject)objOrArr).Properties.GetTemplateByPropertyName(name);
            } else {
                if (!"ElementType".Equals(name)) {
                    generator.ThrowExceptionWithLineInfo(
                           Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                           string.Format(MEMBER_NOT_ELEMENTTYPE, name, typeAssignment.TemplatePath),
                           null,
                           root.CodegenInfo.SourceInfo
                    );
                }
                return ((TObjArr)objOrArr).ElementType;
            }
        }
        
        /// <summary>
        /// Throws an exception if the template is null or not an object or array.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="toVerify"></param>
        /// <param name="typeAssignment"></param>
        /// <param name="root"></param>
        private void VerifyIsObjectOrArray(string name, 
                                           Template toVerify, 
                                           CodeBehindTypeAssignmentInfo typeAssignment, 
                                           Template root) {
            if (toVerify == null) {
                generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                                                     string.Format(MEMBER_NOT_FOUND, name, typeAssignment.TemplatePath),
                                                     null,
                                                     root.CodegenInfo.SourceInfo);
            } else if (!(toVerify is TContainer)) {
                generator.ThrowExceptionWithLineInfo(
                    Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                    string.Format(MEMBER_NOT_OBJ_OR_ARR, name, typeAssignment.TemplatePath),
                    null,
                    root.CodegenInfo.SourceInfo);
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
        /// <param name="typeAssignment">Contains the type (try) to convert to</param>
        /// <returns>
        /// A new template if the conversion is for a primitive value, otherwise 
        /// null (even if the conversion was succesful)
        /// </returns>
        private Template CheckAndProcessTypeConversion(Template template, CodeBehindTypeAssignmentInfo typeAssignment) {
            Template newTemplate = null;
            string typeName = typeAssignment.TypeName;

            switch (template.TemplateTypeId) {
                case TemplateTypeEnum.Decimal:
                    if (IsDoubleType(typeName)) {
                        newTemplate = new TDouble();
                        template.CopyTo(newTemplate);
                        ((TDouble)newTemplate).DefaultValue = Convert.ToDouble(((TDecimal)template).DefaultValue);
                    } else if (!IsDecimalType(typeName)) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             VALID_TYPES_FLOAT,
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
                                                             VALID_TYPES_FLOAT,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    break;
                case TemplateTypeEnum.Long:
                    if (IsDecimalType(typeName)) {
                        newTemplate = new TDecimal();
                        template.CopyTo(newTemplate);
                        ((TDecimal)newTemplate).DefaultValue = Convert.ToDecimal(((TLong)template).DefaultValue);
                    } else if (IsDoubleType(typeName)) {
                        newTemplate = new TDouble();
                        template.CopyTo(newTemplate);
                        ((TDouble)newTemplate).DefaultValue = Convert.ToDouble(((TLong)template).DefaultValue);
                    } else {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             VALID_TYPES_INT,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    break;
                case TemplateTypeEnum.Object:
                    if (((TObject)template).Properties.Count > 0) {
                        generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                             ONLY_UNTYPED_OBJ,
                                                             null,
                                                             template.CodegenInfo.SourceInfo);
                    }
                    template.CodegenInfo.ReuseType = typeName;
                    break;
                default:
                    generator.ThrowExceptionWithLineInfo(Error.SCERRJSONUNSUPPORTEDINSTANCETYPEASSIGNMENT,
                                                         string.Format(INVALID_MEMBER_ASSIGNMENT, 
                                                                       typeName, 
                                                                       template.PropertyName, 
                                                                       typeAssignment.TemplatePath),
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
