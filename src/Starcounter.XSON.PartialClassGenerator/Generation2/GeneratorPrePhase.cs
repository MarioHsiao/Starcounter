using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// 
    /// </summary>
    internal class GeneratorPrePhase {
        private const string MEMBER_NOT_FOUND = "Member '{0}' in path '{1}' was not found.";
        private const string MEMBER_NOT_OBJ_OR_ARR = "Member '{0}' in path '{1}' is not an object or array.";
        private const string MEMBER_NOT_ELEMENTTYPE = "Expected member 'ElementType' but found '{0}' in path '{1}.";
     
        private Gen2DomGenerator generator;

        internal GeneratorPrePhase(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPrePhase(TValue prototype) {
            ProcessBindAssignments(prototype, generator.CodeBehindMetadata);
        }

        /// <summary>
        /// Processes all typeassignments of 'InstanceType' that are in the metadata and makes sure 
        /// that the specified conversions and reuses are valid as well as do the actual conversion.
        /// </summary>
        /// <param name="prototype"></param>
        /// <param name="metadata"></param>
        private void ProcessBindAssignments(TValue prototype, CodeBehindMetadata metadata) {
            foreach (var classInfo in metadata.CodeBehindClasses) {
                // TODO:
                // Due to lack of time for testing, and to want to keep old stuff as is for the moment we
                // will ignore all assignments of Bind if the ExplicitBound<T> interface is not used.
                // These assignments are currently only needed to get correct compilation-errors for
                // explicitly bound properties.
                if(!classInfo.ExplicitlyBound)
                    continue;

                TValue classRoot = generator.FindTemplate(classInfo, prototype);
                foreach (var bindAssignment in classInfo.BindAssignments) {
                    ProcessOneBindAssignment(classRoot, bindAssignment);
                }
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="typeAssignment"></param>
        private void ProcessOneBindAssignment(TValue root, CodeBehindAssignmentInfo bindAssignment) {
            string[] parts = bindAssignment.TemplatePath.Split('.');
            Template theTemplate;   
            Template currentTemplate;

            if (!"DefaultTemplate".Equals(parts?[0])) {
                generator.ThrowExceptionWithLineInfo(Error.SCERRJSONINVALIDBINDASSIGNMENT,
                                                     "Path does not start with field 'DefaultTemplate'",
                                                     null,
                                                     root.CompilerOrigin);
            }

            if (parts.Length == 1) {
                root.Bind = bindAssignment.Value;
            } else {
                VerifyIsObjectOrArray(root.PropertyName, root, bindAssignment, root, Error.SCERRJSONINVALIDBINDASSIGNMENT);

                currentTemplate = root;
                for (int i = 1; i < parts.Length - 1; i++) {
                    currentTemplate = GetChild(currentTemplate, parts[i], bindAssignment, root, Error.SCERRJSONINVALIDBINDASSIGNMENT);
                    VerifyIsObjectOrArray(parts[i], currentTemplate, bindAssignment, root, Error.SCERRJSONINVALIDBINDASSIGNMENT);
                }
                
                theTemplate = GetChild(currentTemplate, parts[parts.Length - 1], bindAssignment, root, Error.SCERRJSONINVALIDBINDASSIGNMENT);
                if (theTemplate == null) {
                    generator.ThrowExceptionWithLineInfo(
                        Error.SCERRJSONINVALIDINSTANCETYPEASSIGNMENT,
                        string.Format(MEMBER_NOT_FOUND, parts[parts.Length - 1], bindAssignment.TemplatePath, Error.SCERRJSONINVALIDBINDASSIGNMENT),
                        null,
                        root.CompilerOrigin
                    );
                }

                ((TValue)theTemplate).Bind = bindAssignment.Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objOrArr"></param>
        /// <param name="name"></param>
        /// <param name="assignment"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        private Template GetChild(Template objOrArr, 
                                  string name,
                                  CodeBehindAssignmentInfo assignment,
                                  Template root,
                                  uint errorCodeOnError) {
            if (objOrArr.TemplateTypeId == TemplateTypeEnum.Object) {
               return ((TObject)objOrArr).Properties.GetTemplateByPropertyName(name);
            } else {
                if (!"ElementType".Equals(name)) {
                    generator.ThrowExceptionWithLineInfo(
                           errorCodeOnError,
                           string.Format(MEMBER_NOT_ELEMENTTYPE, name, assignment.TemplatePath),
                           null,
                           root.CompilerOrigin
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
                                           CodeBehindAssignmentInfo typeAssignment, 
                                           Template root,
                                           uint errorCodeOnError) {
            if (toVerify == null) {
                generator.ThrowExceptionWithLineInfo(errorCodeOnError,
                                                     string.Format(MEMBER_NOT_FOUND, name, typeAssignment.TemplatePath),
                                                     null,
                                                     root.CompilerOrigin);
            } else if (!(toVerify is TContainer)) {
                generator.ThrowExceptionWithLineInfo(
                    errorCodeOnError,
                    string.Format(MEMBER_NOT_OBJ_OR_ARR, name, typeAssignment.TemplatePath),
                    null,
                    root.CompilerOrigin);
            }
        }
    }
}
