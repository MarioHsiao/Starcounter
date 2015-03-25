// ***********************************************************************
// <copyright file="TAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Templates;
using System.Reflection;

namespace Starcounter.Internal.JsonTemplate {
    /// <summary>
    /// The template factory is intended for template parsers as a clean
    /// interface used to built Starcounter controller templates.
    /// It is used as a singleton.
    /// </summary>
    public class TAppFactory<OT, OTT> : ITemplateFactory
        where OT : Json, new()
        where OTT : TObject, new() {
        private static string[] ILLEGAL_PROPERTIES = { "Parent", "Data" };

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
                                                       DebugInfo debugInfo) {
            Template existing;
            ReplaceableTemplate rt;
            String name;

            name = newTemplate.TemplateName;
            existing = parent.Properties.GetTemplateByName(name);

            if (existing != null) {
                rt = existing as ReplaceableTemplate;
                if (rt != null) {
                    if (rt.ConvertTo != null) {
                        if (!(newTemplate is TValue))
                            ErrorHelper.RaiseInvalidTypeConversionError(rt.ConvertTo.CompilerOrigin);

                        newTemplate.CopyTo(rt.ConvertTo);
                        newTemplate = rt.ConvertTo;
                    }

                    CopyReplaceableTemplateValues(rt, newTemplate);
                    parent.Properties.Replace(newTemplate);
                } else {
                    Error.CompileError.Raise<Object>(
                       "A property with the same name already exists.",
                       new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                       debugInfo.FileName
                    );
                }
            } else {
                parent.Properties.Add(newTemplate);
            }
            return newTemplate;
        }

        /// <summary>
        /// Copies the replaceable template values.
        /// </summary>
        /// <param name="rt">The rt.</param>
        /// <param name="newTemplate">The new template.</param>
        private void CopyReplaceableTemplateValues(ReplaceableTemplate rt, Template newTemplate) {
            Boolean boolVal;
            CompilerOrigin co;
            String strVal;

            co = rt.CompilerOrigin;
            MetaTemplate<OT, OTT> tm
                = new MetaTemplate<OT, OTT>(newTemplate, new DebugInfo(co.LineNo, co.ColNo, co.FileName));
            foreach (KeyValuePair<String, Object> value in rt.Values) {

                strVal = value.Value as String;
                if (value.Value == null || strVal != null) {
                    tm.Set(value.Key, strVal);
                    continue;
                }

                boolVal = (bool)value.Value;
                tm.Set(value.Key, boolVal);
            }
        }

        /// <summary>
        /// Gets the meta template.
        /// </summary>
        /// <param name="templ">The templ.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.GetMetaTemplate(object templ, DebugInfo debugInfo) {
            return new MetaTemplate<OT, OTT>((Template)templ, debugInfo);
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
                                                           DebugInfo debugInfo) {
            var appTemplate = (OTT)parent;
            var t = appTemplate.Properties.GetTemplateByName(name);
            if (t == null) {
                // The template is not created yet. This can be because the metadata 
                // is specified before the actual field in the json file.
                // We create a dummy template that will be replaced later.
                // If this dummy template is not replaced later, an exception
                // will be raised.
                t = new ReplaceableTemplate() { TemplateName = name };
                SetCompilerOrigin(t, debugInfo);
                appTemplate.Properties.Add(t);
            }
            return new MetaTemplate<OT, OTT>(t, debugInfo);
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
                                                  DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate<OT, OTT>)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TString() { TemplateName = name };
                appTemplate = (OTT)parent;

                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                if (newTemplate is TString)
                    ((TString)newTemplate).DefaultValue = value;

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
                                                   DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (!(parent is MetaTemplate<OT, OTT>)) {
                newTemplate = new TLong() { TemplateName = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                if (newTemplate is TLong)
                    ((TLong)newTemplate).DefaultValue = value;
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
                                                   DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (!(parent is MetaTemplate<OT, OTT>)) {
                newTemplate = new TDecimal() { TemplateName = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                if (newTemplate is TDecimal)
                    ((TDecimal)newTemplate).DefaultValue = value;
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
                                                  DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (!(parent is MetaTemplate<OT, OTT>)) {
                newTemplate = new TDouble() { TemplateName = name };
                appTemplate = (OTT)parent;

                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                if (newTemplate is TDouble)
                    ((TDouble)newTemplate).DefaultValue = value;

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
                                                   DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (parent is MetaTemplate<OT, OTT>) {
                ((MetaTemplate<OT, OTT>)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TBool() { TemplateName = name };
                appTemplate = (OTT)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                if (newTemplate is TBool)
                    ((TBool)newTemplate).DefaultValue = value;
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
        }

        /// <summary>
        /// Sets the compiler origin.
        /// </summary>
        /// <param name="t">The t.</param>
        /// <param name="d">The d.</param>
        void SetCompilerOrigin(Template t, DebugInfo d) {
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
                                                 DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (parent is MetaTemplate<OT, OTT>) {
                ((MetaTemplate<OT, OTT>)parent).Set(name, value);
                return null;
            }

            // TODO: 
            // It looks a little strange to have this kind of error here, but 
            // since all values that are not handled (like numeric, strings...) 
            // are sent here, the check is done here. Should be changed to make 
            // it a bit more logical.
            if (value != null && !value.Equals("event",
                                               StringComparison.CurrentCultureIgnoreCase)) {
                Error.CompileError.Raise<Object>(
                        "Unknown type '" + value + "' for field '" + name + "'",
                        new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                        debugInfo.FileName
                    );
            }

            newTemplate = new TTrigger() { TemplateName = name };
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
                                                 DebugInfo debugInfo) {
            OTT appTemplate;
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            newTemplate = new TArray<OT>() { TemplateName = name };
            appTemplate = (OTT)parent;
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        /// <summary>
        /// Adds the app property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        object ITemplateFactory.AddAppProperty(object parent, string name, string dotNetName, DebugInfo debugInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            newTemplate = new OTT();
            if (parent != null) {
                newTemplate.TemplateName = name;
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
        object ITemplateFactory.AddAppElement(object array, DebugInfo debugInfo) {
            var newTemplate = new OTT(); // The type of the type array (an TApp)
            newTemplate.Parent = (TContainer)array;
            //			newTemplate.TemplateName = "__ArrayType__"; // All children needs an id
            var arr = ((TObjArr)array);
            arr.ElementType = newTemplate;
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
        object ITemplateFactory.AddCargoProperty(object parent, DebugInfo debugInfo) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds the meta property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        object ITemplateFactory.AddMetaProperty(object template, DebugInfo debugInfo) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the editable property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="b">if set to <c>true</c> [b].</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetEditableProperty(object template, bool b, DebugInfo debugInfo) {
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
                                               DebugInfo debugInfo) {
            ((TObject)template).ClassName = className;
        }

        /// <summary>
        /// Sets the include property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetIncludeProperty(object template,
                                                 string className,
                                                 DebugInfo debugInfo) {
            ((TObject)template).Include = className;
        }

        /// <summary>
        /// Sets the namespace property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetNamespaceProperty(object template,
                                                   string namespaceName,
                                                   DebugInfo debugInfo) {
            ((TObject)template).Namespace = namespaceName;
        }

        /// <summary>
        /// Sets the on update property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetOnUpdateProperty(object template,
                                                  string functionName,
                                                  DebugInfo debugInfo) {
            ((Template)template).OnUpdate = functionName;
        }

        /// <summary>
        /// Sets the bind property.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="path">The path.</param>
        /// <param name="debugInfo">The debug info.</param>
        void ITemplateFactory.SetBindProperty(object template, string path, DebugInfo debugInfo) {
            ((TValue)template).Bind = path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="debugInfo"></param>
        private void VerifyPropertyName(string propertyName, DebugInfo debugInfo) {
            for (int i = 0; i < ILLEGAL_PROPERTIES.Length; i++) {
                if (propertyName.Equals(ILLEGAL_PROPERTIES[i])) {
                    ErrorHelper.RaisePropertyExistsError(propertyName, debugInfo);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Class ErrorHelper
    /// </summary>
    internal static class ErrorHelper {
        internal static void RaisePropertyExistsError(string propertyName, DebugInfo debugInfo) {
            Error.CompileError.Raise<Object>(
                debugInfo.FileName
                + " already contains a definition for '"
                + propertyName
                + "'",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

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
                                                             DebugInfo debugInfo) {
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
        internal static void RaiseInvalidPropertyError(String propertyName, DebugInfo debugInfo) {
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
        internal static void RaiseInvalidTypeConversionError(DebugInfo debugInfo) {
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
        internal static void RaiseInvalidTypeConversionError(CompilerOrigin co) {
            RaiseInvalidTypeConversionError(new DebugInfo(co.LineNo, co.ColNo, co.FileName));
        }

        /// <summary>
        /// Raises the unknown property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseUnknownPropertyError(String propertyName, DebugInfo debugInfo) {
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
        internal static void RaiseUnknownPropertyTypeError(String typeName, DebugInfo debugInfo) {
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
        internal static void RaiseNotImplementedException(String name, DebugInfo debugInfo) {
            Error.CompileError.Raise<Object>(
                "The property '" + name + "' is not implemented yet.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }
    }
}
