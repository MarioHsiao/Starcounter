using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    /// <summary>
    /// Class MetaTemplate
    /// </summary>
    internal class MetaTemplate : Template {
        private static List<string> booleanProperties;
        private static List<string> stringProperties;

        private Starcounter.Templates.Template template;
        private ISourceInfo sourceInfo;

        public override bool IsPrimitive { get { return false; } }
        public override Type MetadataType { get { return null; } }
        public override bool HasInstanceValueOnClient { get { return false; } }
        internal override TemplateTypeEnum TemplateTypeId { get { return TemplateTypeEnum.Unknown; } }

        /// <summary>
        /// Initializes static members of the <see cref="MetaTemplate" /> class.
        /// </summary>
        static MetaTemplate() {
            booleanProperties = new List<string>();
            booleanProperties.Add("EDITABLE");
            booleanProperties.Add("BOUND");

            stringProperties = new List<string>();
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
        internal MetaTemplate(Template template, ISourceInfo sourceInfo) {
            this.template = template;
            this.sourceInfo = sourceInfo;
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
                if (property == null)
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                
                property.Editable = v;
            } else {
                if (stringProperties.Contains(upperName))
                    FactoryExceptionHelper.RaiseWrongValueForPropertyError(name, "string", "boolean", sourceInfo);
                else
                    FactoryExceptionHelper.RaiseUnknownPropertyError(name, sourceInfo);
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

            if (upperName == "CLASS") {
                appTemplate = template as TObject;
                if (appTemplate == null)
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                appTemplate.ClassName = v;
            } else if (upperName == "RUN") {
                actionTemplate = template as TTrigger;
                if (actionTemplate == null)
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                actionTemplate.OnRun = v;
            } else if (upperName == "BIND") {
                valueTemplate = template as TValue;
                if (valueTemplate == null)
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                valueTemplate.Bind = v;
            } else if (upperName == "TYPE") {
                TValue oldProperty = template as TValue;
                if (oldProperty == null || (oldProperty is TObject))
                    FactoryExceptionHelper.RaiseInvalidTypeConversionError(sourceInfo);

                TValue newProperty = GetPropertyFromTypeName(v);
                oldProperty.CopyTo(newProperty);

                var parent = (TObject)oldProperty.Parent;
                parent.Properties.Replace(newProperty);
                template = newProperty;
            } else if (upperName == "REUSE") {
                var tobj = template as TObject;
                if (tobj == null)
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);

                tobj.CodegenMetadata.Add("Reuse", v);
            } else if (upperName == "NAMESPACE") {
                //appTemplate = template as TObject;
                //if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, debugInfo);

                template.Namespace = v;
            } else if (upperName == "DATATYPE") {
                if (template is TObjArr || template is TObject) {
                    template.CodegenMetadata.Add("InstanceDataTypeName", v);
                } else {
                    FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                }
            } else {
                if (booleanProperties.Contains(upperName))
                    FactoryExceptionHelper.RaiseWrongValueForPropertyError(name, "boolean", "string", sourceInfo);
                else
                    FactoryExceptionHelper.RaiseUnknownPropertyError(name, sourceInfo);
            }
        }

        /// <summary>
        /// Gets the template that corresponds to the typename. 
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns></returns>
        private TValue GetPropertyFromTypeName(string typeName) {
            TValue property = null;
            String nameToUpper = typeName.ToUpper();
            switch (nameToUpper) {
                case "DOUBLE":
                case "FLOAT":
                    property = new TDouble();
                    break;
                case "DECIMAL":
                    property = new TDecimal();
                    break;
                case "INT":
                case "INTEGER":
                case "INT32":
                    property = new TLong();
                    break;
                case "STRING":
                    property = new TString();
                    break;
                default:
                    FactoryExceptionHelper.RaiseUnknownPropertyTypeError(typeName, sourceInfo);
                    break;
            }

            if (property != null)
                property.SourceInfo = sourceInfo;

            return property;
        }
    }
}
