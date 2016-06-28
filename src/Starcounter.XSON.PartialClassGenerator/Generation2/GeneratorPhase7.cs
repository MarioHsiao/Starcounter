using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Metadata;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Binds properties that either have a binding specified that match
    /// a property in code-behind, or have declared a property in code-behind with
    /// the same name (and with a supported returntype) as a property in the template.
    /// </summary>
    /// <remarks>
    /// The bindings created here will be treated a bit different than ordinary databindings
    /// since they are always valid and will never be 
    /// </remarks>
    internal class GeneratorPhase7 {
        private const StringComparison COMPARE = StringComparison.InvariantCultureIgnoreCase;
        private Gen2DomGenerator generator;
        
        internal GeneratorPhase7(Gen2DomGenerator generator) {
            this.generator = generator;
        }

        internal void RunPhase7(AstJsonClass rootJsonClass) {
            CreateBindingsToCodeBehind(rootJsonClass);
        }

        private void CreateBindingsToCodeBehind(AstJsonClass jsonClass) {
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
                    
                    if (codeBehindProperty == null) {
                        // No property in code-behind with the same name. Lets check if we find 
                        // one using the Bind-value from the template.

                        // TODO:
                        // If we use the new pattern of setting the properties directly on the template
                        // from the code-behind file, the Bind-property will not be set here. 
                        // Should we only allow binding to code-behind using 

                        bindingName = ((TValue)property.Template).Bind;
                        if (!bindingName.Equals(property.MemberName, COMPARE)) {
                            suppressGenerateProperty = false;
                            codeBehindProperty = jsonClass.CodebehindClass.PropertyList.Find((item) => {
                                return (item.Name.Equals(bindingName, COMPARE));
                            });
                        }
                    }

                    if (codeBehindProperty != null) {
                        // A property in the code-behind exists

                        if (suppressGenerateProperty) {

                        }


                        

                    }
                }

                if (child is AstJsonClass) {
                    CreateBindingsToCodeBehind((AstJsonClass)child);
                }
            }
        }
    }
}
