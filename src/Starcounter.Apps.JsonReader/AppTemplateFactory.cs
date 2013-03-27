// ***********************************************************************
// <copyright file="TAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonTemplate
{
    internal abstract class MetaTemplate {
    }

    /// <summary>
    /// Class MetaTemplate
    /// </summary>
    internal class MetaTemplate<OT,OTT> : MetaTemplate
        where OT : Obj, new()
        where OTT : TObj, new() 
    {
        /// <summary>
        /// The _boolean properties
        /// </summary>
        private static List<String> _booleanProperties;
        /// <summary>
        /// The _string properties
        /// </summary>
        private static List<String> _stringProperties;

        /// <summary>
        /// Initializes static members of the <see cref="MetaTemplate" /> class.
        /// </summary>
        static MetaTemplate()
        {
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
        /// The _template
        /// </summary>
        private Starcounter.Templates.Template _template;
        /// <summary>
        /// The _debug info
        /// </summary>
        private DebugInfo _debugInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaTemplate" /> class.
        /// </summary>
        /// <param name="forTemplate">For template.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal MetaTemplate(Template forTemplate, DebugInfo debugInfo)
        {
            _template = forTemplate;
            _debugInfo = debugInfo;
        }

        /// <summary>
        /// Sets the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="v">if set to <c>true</c> [v].</param>
        public void Set(string name, bool v)
        {
            TValue property;
            String upperName;
            ReplaceableTemplate rt;

            rt = _template as ReplaceableTemplate;
            if (rt != null)
            {
                // If the template is a RepleableTemplate we just store the value
                // and set it later when the template is replaced with the correct
                // one. By doing this we get the checks for correct type.
                rt.SetValue(name, v);              
                return;
            }

            upperName = name.ToUpper();
            if (upperName == "EDITABLE")
            {
                property = _template as TValue;
                if (property == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                property.Editable = v;
            }
            else
            {
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
        public void Set(string name, string v)
        {
            TTrigger actionTemplate;
            OTT appTemplate;
            TValue valueTemplate;
            String upperName;
            ReplaceableTemplate rt;

            upperName = name.ToUpper();
            rt = _template as ReplaceableTemplate;
            if (rt != null)
            {
                if (upperName != "TYPE") 
                    rt.SetValue(name, v);
                else 
                    rt.ConvertTo = GetPropertyFromTypeName(v);

                return;
            }

            if (upperName == "UPDATE")
            {
                valueTemplate = _template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                valueTemplate.OnUpdate = v;
            }
            else if (upperName == "CLASS")
            {
                appTemplate = _template as OTT;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                ((TObj)_template).ClassName = v;
            }
            else if (upperName == "RUN")
            {
                actionTemplate = _template as TTrigger;
                if (actionTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                actionTemplate.OnRun = v;
            }
            else if (upperName == "BIND")
            {
                valueTemplate = _template as TValue;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                valueTemplate.Bind = v;
                valueTemplate.Bound = true;
            }
            else if (upperName == "TYPE")
            {
                TValue oldProperty = _template as TValue;
                if (oldProperty == null || (oldProperty is TObj)) 
                    ErrorHelper.RaiseInvalidTypeConversionError(_debugInfo);

                TValue newProperty = GetPropertyFromTypeName(v);
                oldProperty.CopyTo(newProperty);

                TObj parent = (TObj)oldProperty.Parent;
                parent.Properties.Replace(newProperty);
            }
            else if (upperName == "REUSE")
            {
                ErrorHelper.RaiseNotImplementedException(name, _debugInfo);
            }
            else if (upperName == "NAMESPACE")
            {
                appTemplate = _template as OTT;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                appTemplate.Namespace = v;
            } else if (upperName == "DATATYPE") {
                System.Diagnostics.Debugger.Launch();

                if (_template is TObjArr) {
                    appTemplate = ((TObjArr)_template).App as OTT;
                } else {
                    appTemplate = _template as OTT;
                }

                if (appTemplate == null)
                    ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                appTemplate.InstanceDataTypeName = v;
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
        private TValue GetPropertyFromTypeName(string v)
        {
            TValue p = null;
            String nameToUpper = v.ToUpper();
            switch (nameToUpper)
            {
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
        private void SetCompilerOrigin(Template t, DebugInfo d)
        {
            t.CompilerOrigin.LineNo = d.LineNo;
            t.CompilerOrigin.ColNo = d.ColNo;
            t.CompilerOrigin.FileName = d.FileName;
        }
    }

    /// <summary>
    /// The template factory is intended for template parsers as a clean
    /// interface used to built Starcounter controller templates.
    /// It is used as a singleton.
    /// </summary>
    public class TAppFactory<OT,OTT> : ITemplateFactory
        where OT : Obj, new()
        where OTT : TObj, new() 
    {
        /// <summary>
        /// Checks if the specified name already exists. If the name exists
        /// and is not used by an ReplaceableTemplate an exception is thrown.
        /// In case the existing template is an ReplaceableTemplate all values
        /// set on it are copied to the new template, and the ReplaceableTemplate is
        /// replaced with the new template.
        /// If no template exists, the new template is added to the parent.
        /// </summary>
        /// <param name="newTemplate">The new template.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>Template.</returns>
        private Template CheckAndAddOrReplaceTemplate(Template newTemplate, 
                                                       OTT parent,
                                                       DebugInfo debugInfo)
        {
            Template existing;
            ReplaceableTemplate rt;
            String name;

            name = newTemplate.Name;
            existing = parent.Properties.GetTemplateByName(name);

            if (existing != null)
            {
                rt = existing as ReplaceableTemplate;
                if (rt != null)
                {
                    if (rt.ConvertTo != null)
                    {
                        if (!(newTemplate is TValue)) 
                            ErrorHelper.RaiseInvalidTypeConversionError(rt.ConvertTo.CompilerOrigin);

                        newTemplate.CopyTo(rt.ConvertTo);
                        newTemplate = rt.ConvertTo;
                    }

                    CopyReplaceableTemplateValues(rt, newTemplate);
                    parent.Properties.Replace(newTemplate);
                }
                else
                {
                    Error.CompileError.Raise<Object>(
                       "A property with the same name already exists.",
                       new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                       debugInfo.FileName
                    );
                }
            }
            else
            {
                parent.Properties.Add(newTemplate);
            }
            return newTemplate;
        }

        /// <summary>
        /// Copies the replaceable template values.
        /// </summary>
        /// <param name="rt">The rt.</param>
        /// <param name="newTemplate">The new template.</param>
        private void CopyReplaceableTemplateValues(ReplaceableTemplate rt, Template newTemplate)
        {
            Boolean boolVal;
            CompilerOrigin co;
            String strVal;

            co = rt.CompilerOrigin;
            MetaTemplate<OT,OTT> tm 
                = new MetaTemplate<OT,OTT>(newTemplate, new DebugInfo(co.LineNo, co.ColNo, co.FileName));
            foreach (KeyValuePair<String, Object> value in rt.Values)
            {
                strVal = value.Value as String;
                if (strVal != null)
                {
                    tm.Set(value.Key, strVal);
                    continue;
                }

                boolVal = (Boolean)value.Value;
                tm.Set(value.Key, boolVal);
            }
        }

        /// <summary>
        /// Gets the meta template.
        /// </summary>
        /// <param name="templ">The templ.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.GetMetaTemplate(object templ, DebugInfo debugInfo)
        {
            return new MetaTemplate<OT,OTT>((Template)templ, debugInfo);
        }

        /// <summary>
        /// Gets the meta template for property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.GetMetaTemplateForProperty(object parent, 
                                                           string name,
                                                           DebugInfo debugInfo)
        {
            var appTemplate = (OTT)parent;
            var t = appTemplate.Properties.GetTemplateByName(name);
            if (t == null)
            {
                // The template is not created yet. This can be because the metadata 
                // is specified before the actual field in the json file.
                // We create a dummy template that will be replaced later.
                // If this dummy template is not replaced later, an exception
                // will be raised.
                t = new ReplaceableTemplate() { Name = name };
                SetCompilerOrigin(t, debugInfo);
                appTemplate.Properties.Add(t);
            }
            return new MetaTemplate<OT,OTT>(t, debugInfo);
        }

        /// <summary>
        /// Adds the string property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        ///
        object ITemplateFactory.AddTString(object parent,
                                                  string name,
                                                  string dotNetName,
                                                  string value,
                                                  DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (parent is MetaTemplate)
            {
                ((MetaTemplate<OT,OTT>)parent).Set(name, value);
                return null;
            }
            else
            {
                newTemplate = new TString() { Name = name };
                appTemplate = (OTT)parent;

                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
        }

        /// <summary>
        /// Adds the integer property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddIntegerProperty(object parent,
                                                   string name,
                                                   string dotNetName,
                                                   int value,
                                                   DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate<OT,OTT>))
            {
                newTemplate = new TLong() { Name = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        /// <summary>
        /// Adds the decimal property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddTDecimal(object parent,
                                                   string name,
                                                   string dotNetName,
                                                   decimal value,
                                                   DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate<OT,OTT>))
            {
                newTemplate = new TDecimal() { Name = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        /// <summary>
        /// Adds the double property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddTDouble(object parent,
                                                  string name,
                                                  string dotNetName,
                                                  double value,
                                                  DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate<OT,OTT>))
            {
                newTemplate = new TDouble() { Name = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        /// <summary>
        /// Adds the boolean property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddBooleanProperty(object parent,
                                                   string name,
                                                   string dotNetName,
                                                   bool value,
                                                   DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (parent is MetaTemplate<OT,OTT>)
            {
                ((MetaTemplate<OT,OTT>)parent).Set(name, value);
                return null;
            }
            else
            {
                newTemplate = new TBool() { Name = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
        }

        /// <summary>
        /// Sets the compiler origin.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="d">The d.</param>
        void SetCompilerOrigin(Template t, DebugInfo d)
        {
            t.CompilerOrigin.FileName = d.FileName;
            t.CompilerOrigin.LineNo = d.LineNo;
            t.CompilerOrigin.ColNo = d.ColNo;
        }

        /// <summary>
        /// Adds the event property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddEventProperty(object parent,
                                                 string name,
                                                 string dotNetName,
                                                 string value,
                                                 DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            if (parent is MetaTemplate<OT, OTT>) {
                ((MetaTemplate<OT,OTT>)parent).Set(name, value);
                return null;
            }

            // TODO: 
            // It looks a little strange to have this kind of error here, but 
            // since all values that are not handled (like numeric, strings...) 
            // are sent here, the check is done here. Should be changed to make 
            // it a bit more logical.
            if (value != null && !value.Equals("event",
                                               StringComparison.CurrentCultureIgnoreCase))
            {
                Error.CompileError.Raise<Object>(
                        "Unknown type '" + value + "' for field '" + name + "'",
                        new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                        debugInfo.FileName
                    );
            }

            newTemplate = new TTrigger() { Name = name };
            appTemplate = (OTT)parent;
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        /// <summary>
        /// Adds the array property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddArrayProperty(object parent,
                                                 string name, string dotNetName,
                                                 DebugInfo debugInfo)
        {
            OTT appTemplate;
            Template newTemplate;

            newTemplate = new TArr<OT, OTT>() { Name = name };
            appTemplate = (OTT)parent;
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

//        object ITemplateFactory.AddObjectProperty(object parent,
//                                                  string name,
//                                                  DebugInfo debugInfo)
//        {
//            TApp appTemplate;
//            Template newTemplate;
//
//            newTemplate = new ObjectProperty();
//            appTemplate = parent as TApp;
//            if (parent != null)
//            {
//                newTemplate.Name = name;
//                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
//            }
//            SetCompilerOrigin(newTemplate, debugInfo);
//            return newTemplate;
//        }

        /// <summary>
        /// Adds the app property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddAppProperty(object parent, string name, string dotNetName, DebugInfo debugInfo)
        {
            Template newTemplate;

            newTemplate = new OTT();
            if (parent != null)
            {
                newTemplate.Name = name;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, (OTT)parent, debugInfo);
            }
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        /// <summary>
        /// Adds the app element.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddAppElement(object array, DebugInfo debugInfo)
        {
            var newTemplate = new OTT(); // The type of the type array (an TApp)
            newTemplate.Parent = (TContainer)array;
            //			newTemplate.Name = "__ArrayType__"; // All children needs an id
            var arr = ((TObjArr)array);
            arr.App = newTemplate;
            newTemplate.Parent = arr;
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        /// <summary>
        /// Adds the cargo property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        object ITemplateFactory.AddCargoProperty(object parent, DebugInfo debugInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the meta property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        object ITemplateFactory.AddMetaProperty(object template, DebugInfo debugInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the editable property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="b">if set to <c>true</c> [b].</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetEditableProperty(object template, bool b, DebugInfo debugInfo)
        {
            ((Template)template).Editable = b;
        }

        /// <summary>
        /// Sets the class property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetClassProperty(object template,
                                               string className,
                                               DebugInfo debugInfo)
        {
            ((TObj)template).ClassName = className;
        }

        /// <summary>
        /// Sets the include property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetIncludeProperty(object template,
                                                 string className,
                                                 DebugInfo debugInfo)
        {
            ((TObj)template).Include = className;
        }

        /// <summary>
        /// Sets the namespace property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetNamespaceProperty(object template,
                                                   string namespaceName,
                                                   DebugInfo debugInfo)
        {
            ((TObj)template).Namespace = namespaceName;
        }

        /// <summary>
        /// Sets the on update property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetOnUpdateProperty(object template, 
                                                  string functionName, 
                                                  DebugInfo debugInfo)
        {
            ((Template)template).OnUpdate = functionName;
        }

        /// <summary>
        /// Sets the bind property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="path">The path.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetBindProperty(object template, string path, DebugInfo debugInfo)
        {
            ((TValue)template).Bind = path;
        }
    }

    /// <summary>
    /// Class ErrorHelper
    /// </summary>
    internal static class ErrorHelper
    {
        /// <summary>
        /// Raises the wrong value for property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="foundType">Type of the found.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseWrongValueForPropertyError(String propertyName, 
                                                             String expectedType, 
                                                             String foundType, 
                                                             DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Wrong value for the property '" + propertyName  
                    + "'. Expected a " + expectedType 
                    + " but found a " + foundType,
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        /// <summary>
        /// Raises the invalid property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseInvalidPropertyError(String propertyName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Property '" + propertyName + "' is not valid on this field.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        /// <summary>
        /// Raises the invalid type conversion error.
        /// </summary>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseInvalidTypeConversionError(DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Invalid field for Type property. Valid fields are string, int, decimal, double and boolean.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        /// <summary>
        /// Raises the invalid type conversion error.
        /// </summary>
        /// <param name="co">The co.</param>
        internal static void RaiseInvalidTypeConversionError(CompilerOrigin co)
        {
            RaiseInvalidTypeConversionError(new DebugInfo(co.LineNo, co.ColNo, co.FileName));
        }

        /// <summary>
        /// Raises the unknown property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseUnknownPropertyError(String propertyName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Unknown property '" + propertyName + "'.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        /// <summary>
        /// Raises the unknown property type error.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseUnknownPropertyTypeError(String typeName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Unknown type '" + typeName + "'.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        /// <summary>
        /// Raises the not implemented exception.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseNotImplementedException(String name, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "The property '" + name + "' is not implemented yet.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }
    }
}
