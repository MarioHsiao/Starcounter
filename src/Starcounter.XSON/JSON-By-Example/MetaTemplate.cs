using System;
using System.Collections.Generic;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonTemplate {
    internal abstract class MetaTemplate {
    }

    /// <summary>
    /// Class MetaTemplate
    /// </summary>
    internal class MetaTemplate<OT, OTT> : MetaTemplate
        where OT : Json, new()
        where OTT : TObject, new() {

        private static List<String> _booleanProperties;
        private static List<String> _stringProperties;

        private Starcounter.Templates.Template _template;
        private DebugInfo _debugInfo;

        /// <summary>
        /// Initializes static members of the <see cref="MetaTemplate" /> class.
        /// </summary>
        static MetaTemplate() {
            _booleanProperties = new List<String>();
            _booleanProperties.Add("EDITABLE");
            _booleanProperties.Add("BOUND");

            _stringProperties = new List<String>();
            _stringProperties.Add("UPDATE");
            _stringProperties.Add("CLASS");
            _stringProperties.Add("RUN");
            _stringProperties.Add("BIND");
            _stringProperties.Add("TYPE");
            _stringProperties.Add("REUSE");
            _stringProperties.Add("NAMESPACE");
            _stringProperties.Add("DATATYPE");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaTemplate" /> class.
        /// </summary>
        /// <param name="forTemplate">For template.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal MetaTemplate(Template forTemplate, DebugInfo debugInfo) {
            _template = forTemplate;
            _debugInfo = debugInfo;
        }

        /// <summary>
        /// Sets the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="v">if set to <c>true</c> [v].</param>
        public void Set(string name, bool v) {
            TValue property;
            String upperName;
            ReplaceableTemplate rt;

            rt = _template as ReplaceableTemplate;
            if (rt != null) {
                // If the template is a RepleableTemplate we just store the value
                // and set it later when the template is replaced with the correct
                // one. By doing this we get the checks for correct type.
                rt.SetValue(name, v);
                return;
            }

            upperName = name.ToUpper();
            if (upperName == "EDITABLE") {
                property = _template as TValue;
                if (property == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                property.Editable = v;
            } else {
                if (_stringProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "string", "boolean", _debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, _debugInfo);
            }
        }

        /// <summary>
        /// Sets the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="v">The v.</param>
        public void Set(string name, string v) {
            TTrigger actionTemplate;
            OTT appTemplate;
            TValue valueTemplate;
            String upperName;
            ReplaceableTemplate rt;

            upperName = name.ToUpper();
            rt = _template as ReplaceableTemplate;
            if (rt != null) {
                if (upperName != "TYPE")
                    rt.SetValue(name, v);
                else
                    rt.ConvertTo = GetPropertyFromTypeName(v);

                return;
            }

            if (upperName == "UPDATE") {
                valueTemplate = _template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                valueTemplate.OnUpdate = v;
            } else if (upperName == "CLASS") {
                appTemplate = _template as OTT;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                ((TObject)_template).ClassName = v;
            } else if (upperName == "RUN") {
                actionTemplate = _template as TTrigger;
                if (actionTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                actionTemplate.OnRun = v;
            } else if (upperName == "BIND") {
                valueTemplate = _template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                valueTemplate.Bind = v;
            } else if (upperName == "TYPE") {
                TValue oldProperty = _template as TValue;
                if (oldProperty == null || (oldProperty is TObject))
                    ErrorHelper.RaiseInvalidTypeConversionError(_debugInfo);

                TValue newProperty = GetPropertyFromTypeName(v);
                oldProperty.CopyTo(newProperty);

                var parent = (TObject)oldProperty.Parent;
                parent.Properties.Replace(newProperty);
                _template = newProperty;
            } else if (upperName == "REUSE") {
                var tobj = _template as TObject;
                if (tobj == null)
                    ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                tobj.CodegenMetadata.Add("Reuse", v);
            } else if (upperName == "NAMESPACE") {
                appTemplate = _template as OTT;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                appTemplate.Namespace = v;
            } else if (upperName == "DATATYPE") {
                if (_template is TObjArr || _template is OTT) {
                    _template.CodegenMetadata.Add("InstanceDataTypeName", v);
                } else {
                    ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                }
            } else {
                if (_booleanProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "boolean", "string", _debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, _debugInfo);
            }
        }

        /// <summary>
        /// Gets the name of the property from type.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>Property.</returns>
        private TValue GetPropertyFromTypeName(string v) {
            TValue p = null;
            String nameToUpper = v.ToUpper();
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
                    ErrorHelper.RaiseUnknownPropertyTypeError(v, _debugInfo);
                    break;
            }

            if (p != null)
                SetCompilerOrigin(p, _debugInfo);

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
