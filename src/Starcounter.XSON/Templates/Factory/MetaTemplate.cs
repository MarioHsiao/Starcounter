using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    /// <summary>
    /// Class MetaTemplate
    /// </summary>
    internal class MetaTemplate : Template {
        private static List<string> allowedProperties;

        private Template template;
        private ISourceInfo sourceInfo;

        public override bool IsPrimitive { get { return false; } }
        public override Type MetadataType { get { return null; } }
        public override bool HasInstanceValueOnClient { get { return false; } }
        internal override TemplateTypeEnum TemplateTypeId { get { return TemplateTypeEnum.Unknown; } }

        /// <summary>
        /// Initializes static members of the <see cref="MetaTemplate" /> class.
        /// </summary>
        static MetaTemplate() {
            allowedProperties = new List<string>();
            allowedProperties.Add("BIND");
            allowedProperties.Add("REUSE");
            allowedProperties.Add("NAMESPACE");
            allowedProperties.Add("DATATYPE");
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
        /// Sets the string on the template for the property with the 
        /// specified name.
        /// </summary>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="v">The value.</param>
        internal void Set(string name, object value) {
            TValue valueTemplate;
            string upperName;
            ReplaceableTemplate rt;
            
            upperName = name.ToUpper();
            if (!allowedProperties.Contains(upperName))
                FactoryExceptionHelper.ThrowInvalidMetadataProperty(name, sourceInfo);

            if (!(value is string))
                FactoryExceptionHelper.RaiseWrongValueForPropertyError(name, "string", value.GetType().Name, sourceInfo);
            
            rt = template as ReplaceableTemplate;
            if (rt != null) {
                rt.SetValue(name, (string)value);
            } else {
                if (upperName == "BIND") {
                    valueTemplate = template as TValue;
                    if (valueTemplate == null)
                        FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                    valueTemplate.Bind = (string)value;
                } else if (upperName == "REUSE") {
                    var tobj = template as TObject;
                    if (tobj == null)
                        FactoryExceptionHelper.RaiseInvalidPropertyError(name, sourceInfo);
                    tobj.CodegenInfo.ReuseType = (string)value;
                } else if (upperName == "NAMESPACE") {
                    template.CodegenInfo.Namespace = (string)value;
                } else if (upperName == "DATATYPE") {
                    template.CodegenInfo.BoundToType = (string)value;
                }
            }
        }
    }
}
