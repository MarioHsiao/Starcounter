using System;
using System.Collections.Generic;
using Starcounter.Templates;

namespace Starcounter.XSON.JSONByExample {
    /// <summary>
    /// Class MetaTemplate
    /// </summary>
    internal class MetaTemplate {
        private static List<string> booleanProperties;
        private static List<string> stringProperties;

        private Starcounter.Templates.Template template;
        private DebugInfo debugInfo;

        /// <summary>
        /// Initializes static members of the <see cref="MetaTemplate" /> class.
        /// </summary>
        static MetaTemplate() {
            booleanProperties = new List<string>();
            booleanProperties.Add("EDITABLE");
            booleanProperties.Add("BOUND");

            stringProperties = new List<string>();
            stringProperties.Add("UPDATE");
            stringProperties.Add("CLASS");
            stringProperties.Add("RUN");
            stringProperties.Add("BIND");
            stringProperties.Add("TYPE");
            stringProperties.Add("REUSE");
            stringProperties.Add("NAMESPACE");
            stringProperties.Add("DATATYPE");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaTemplate" /> class.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="debugInfo"></param>
        internal MetaTemplate(Template template, DebugInfo debugInfo) {
            this.template = template;
            this.debugInfo = debugInfo;
        }

        /// <summary>
        /// Sets the boolean value on the template for the property with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="v">if set to <c>true</c> [v].</param>
        internal void Set(string name, bool v) {
            TValue property;
            String upperName;
            ReplaceableTemplate rt;

            rt = template as ReplaceableTemplate;
            if (rt != null) {
                // If the template is a RepleableTemplate we just store the value
                // and set it later when the template is replaced with the correct
                // one. By doing this we get the checks for correct type.
                rt.SetValue(name, v);
                return;
            }

            upperName = name.ToUpper();
            if (upperName == "EDITABLE") {
                property = template as TValue;
                if (property == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);

                property.Editable = v;
            } else {
                if (stringProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "string", "boolean", debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, debugInfo);
            }
        }

        /// <summary>
        /// Sets the string on the template for the property with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="v">The value.</param>
        internal void Set(string name, string v) {
            TTrigger actionTemplate;
            TObject appTemplate;
            TValue valueTemplate;
            String upperName;
            ReplaceableTemplate rt;

            upperName = name.ToUpper();
            rt = template as ReplaceableTemplate;
            if (rt != null) {
                if (upperName != "TYPE")
                    rt.SetValue(name, v);
                else
                    rt.ConvertTo = GetPropertyFromTypeName(v);

                return;
            }

            if (upperName == "UPDATE") {
                valueTemplate = template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);

                valueTemplate.OnUpdate = v;
            } else if (upperName == "CLASS") {
                appTemplate = template as TObject;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);
                appTemplate.ClassName = v;
            } else if (upperName == "RUN") {
                actionTemplate = template as TTrigger;
                if (actionTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);
                actionTemplate.OnRun = v;
            } else if (upperName == "BIND") {
                valueTemplate = template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);
                valueTemplate.Bind = v;
            } else if (upperName == "TYPE") {
                TValue oldProperty = template as TValue;
                if (oldProperty == null || (oldProperty is TObject))
                    ErrorHelper.RaiseInvalidTypeConversionError(debugInfo);

                TValue newProperty = GetPropertyFromTypeName(v);
                oldProperty.CopyTo(newProperty);

                var parent = (TObject)oldProperty.Parent;
                parent.Properties.Replace(newProperty);
                template = newProperty;
            } else if (upperName == "REUSE") {
                var tobj = template as TObject;
                if (tobj == null)
                    ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);

                tobj.CodegenMetadata.Add("Reuse", v);
            } else if (upperName == "NAMESPACE") {
                //appTemplate = template as TObject;
                //if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);

                template.Namespace = v;
            } else if (upperName == "DATATYPE") {
                if (template is TObjArr || template is TObject) {
                    template.CodegenMetadata.Add("InstanceDataTypeName", v);
                } else {
                    ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);
                }
            } else {
                if (booleanProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "boolean", "string", debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, debugInfo);
            }
        }

        /// <summary>
        /// Gets the template that corresponds to the typename. 
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns></returns>
        private TValue GetPropertyFromTypeName(string typeName) {
            TValue p = null;
            String nameToUpper = typeName.ToUpper();
            switch (nameToUpper) {
                case "DOUBLE":
                case "FLOAT":
                    p = new TDouble();
                    break;
                case "DECIMAL":
                    p = new TDecimal();
                    break;
                case "INT":
                case "INTEGER":
                case "INT32":
                    p = new TLong();
                    break;
                case "STRING":
                    p = new TString();
                    break;
                default:
                    ErrorHelper.RaiseUnknownPropertyTypeError(typeName, debugInfo);
                    break;
            }

            if (p != null)
                SetCompilerOrigin(p, debugInfo);

            return p;
        }

        /// <summary>
        /// Sets the compiler origin.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="d">The d.</param>
        private void SetCompilerOrigin(Template t, DebugInfo d) {
            t.CompilerOrigin.LineNo = d.LineNo;
            t.CompilerOrigin.ColNo = d.ColNo;
            t.CompilerOrigin.FileName = d.FileName;
        }
    }
}
