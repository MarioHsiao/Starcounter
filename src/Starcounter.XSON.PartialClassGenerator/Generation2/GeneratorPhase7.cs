using System;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Maps existing properties in code-behind with properties coming from the template, and if property 
    /// already exists, marks the AstProperty which will lead to that no accessor-property will be generated.
    /// This will allow automatic binding to code-behind.
    /// </summary>
    internal class GeneratorPhase7 {
        private const StringComparison COMPARE = StringComparison.InvariantCultureIgnoreCase;
        private Gen2DomGenerator generator;
        
        internal GeneratorPhase7(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase7(AstJsonClass rootJsonClass) {
            SuppressExistingCodeBehindAccessorProperties(rootJsonClass);
        }

        private void SuppressExistingCodeBehindAccessorProperties(AstJsonClass jsonClass) {
            AstProperty property;
            CodeBehindClassInfo codeBehindClass;
            CodeBehindPropertyInfo codeBehindProperty;
            string bindingName;
            bool suppressGenerateProperty;

            codeBehindClass = jsonClass.CodebehindClass;
            foreach (AstBase child in jsonClass.Children) {
                property = child as AstProperty;
                if (codeBehindClass != null && property != null) {
                    suppressGenerateProperty = true;
                    bindingName = property.MemberName;
                    codeBehindProperty = jsonClass.CodebehindClass.PropertyList.Find((item) => {
                        return (item.Name.Equals(bindingName, COMPARE));
                    });
                    
                    // TODO:
                    // Not needed in the first implementation. Since we only suppress the accessor-property
                    // we don't care to check other bindings to code-behind. 

                    //if (codeBehindProperty == null) {
                    //    // No property in code-behind with the same name. Lets check if we find 
                    //    // one using the Bind-value from the template.
                        
                    //    bindingName = ((TValue)property.Template).Bind;
                    //    if (!bindingName.Equals(property.MemberName, COMPARE)) {
                    //        suppressGenerateProperty = false;
                    //        codeBehindProperty = jsonClass.CodebehindClass.PropertyList.Find((item) => {
                    //            return (item.Name.Equals(bindingName, COMPARE));
                    //        });
                    //    }
                    //}

                    if (codeBehindProperty != null) {
                        // A property in the code-behind exists

                        // TODO:
                        // suppressGenerateProperty will always be true in this version, but kept to
                        // remember when we change the databindings.
                        if (suppressGenerateProperty) {
                            property.GenerateAccessorProperty = false;
                            ((TValue)property.Template).BindingStrategy = BindingStrategy.Bound;

                            // Find the corresponding property in the schema (constructor)
                            // The astnode for the constructor is implemented incorrectly so we
                            // have to do a workaround to change the correct property.
                            var schemaClass = jsonClass.NTemplateClass as AstSchemaClass;
                            if (schemaClass != null) {
                                var schemaProp = (AstProperty)schemaClass.Constructor.Children.Find((item) => {
                                    var prop = item as AstProperty;
                                    if (prop != null)
                                        return (prop.MemberName.Equals(bindingName, COMPARE));
                                    return false;
                                });
                                if (schemaProp != null) 
                                    schemaProp.GenerateAccessorProperty = false;
                                
                            }
                        }
                    }
                }

                if (child is AstJsonClass) {
                    SuppressExistingCodeBehindAccessorProperties((AstJsonClass)child);
                }
            }
        }
    }
}
