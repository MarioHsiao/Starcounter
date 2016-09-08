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
using System.Text.RegularExpressions;
using System.Text;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.JSONByExample {
    /// <summary>
    /// The template factory is intended for template parsers as a clean
    /// interface used to built Starcounter controller templates.
    /// It is used as a singleton.
    /// </summary>
    public class TAppFactory<TJson, TTemplate> : ITemplateFactory 
        where TJson : Json, new()
        where TTemplate : TValue {
        private static string[] ILLEGAL_PROPERTIES = { "parent", "data", "input" };
        private static Regex legalPropertyNameRegex = new Regex(@"[_a-zA-Z][\w]*");

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
        private Template CheckAndAddOrReplaceTemplate(Template newTemplate, object parent, ISourceInfo debugInfo) {
            Template existing;
            ReplaceableTemplate rt;
            String name;
            TObject objParent;
            TObjArr arrParent;

            if (parent == null) {
                // HACK HACK HACK.
                // There is no way to easily set metadata on single value json today so we set editable to true per default.
                newTemplate.Editable = true;
                return newTemplate;
            }

            name = newTemplate.PropertyName;

            if (parent is TObject) {
                objParent = (TObject)parent;
                existing = objParent.Properties.GetTemplateByPropertyName(name);

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
                        objParent.Properties.Replace(newTemplate);
                    } else {
                        Error.CompileError.Raise<Object>(
                           "A property with the same name already exists.",
                           new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                           debugInfo.FileName
                        );
                    }
                } else {
                    objParent.Properties.Add(newTemplate);
                }
            } else if (parent is TObjArr) {
                arrParent = (TObjArr)parent;
                existing = arrParent.ElementType;
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
                    }
                }
                
                arrParent.ElementType = (TValue)newTemplate;
                newTemplate.Parent = arrParent;
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
            MetaTemplate tm = new MetaTemplate(newTemplate, new DebugInfo(co.LineNo, co.ColNo, co.FileName));
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
            return new MetaTemplate((Template)templ, debugInfo);
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
            if (parent == null)
                return null;

            var appTemplate = parent as TObject;

            var propertyName = name;
            if (name.EndsWith("$")) {
                propertyName = name.Substring(0, name.Length - 1);
//                ErrorHelper.RaiseInvalidEditableFlagForMetadata(name.Substring(0, name.Length - 1), debugInfo);
            }

            var t = appTemplate.Properties.GetTemplateByPropertyName(propertyName);
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
            return new MetaTemplate(t, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TString() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);
            if (!(parent is MetaTemplate)) {
                newTemplate = new TLong() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (!(parent is MetaTemplate)) {
                newTemplate = new TDecimal() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (!(parent is MetaTemplate)) {
                newTemplate = new TDouble() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);
            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TBool() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
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
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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
            Template newTemplate;

            VerifyPropertyName(dotNetName, debugInfo);

            newTemplate = new TArray<TJson>() { TemplateName = name };
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
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

            newTemplate = new TObject();
            if (parent != null) {
                newTemplate.TemplateName = name;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, debugInfo);
            }
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

        private void VerifyPropertyName(string propertyName, DebugInfo debugInfo) {
            if (propertyName == null)
                return;

            CheckForInvalidCharactersInPropertyName(propertyName, debugInfo);
            CheckForIllegalPropertyName(propertyName, debugInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="debugInfo"></param>
        private void CheckForInvalidCharactersInPropertyName(string propertyName, DebugInfo debugInfo) {
            Match match;
            MatchCollection matches;
            StringBuilder invalidTokens;
            int tokenStart;
            int tokenLength;
            
            matches = legalPropertyNameRegex.Matches(propertyName);
            if (matches.Count > 0) {
                match = matches[0];

                if (match.Length == propertyName.Length)
                    return; // Valid name!

                invalidTokens = new StringBuilder();

                // Do we have the first match in the beginning or not?
                if (match.Index == 0) {
                    // First invalid token is after the first match (possibly in the end of the name).
                    tokenStart = match.Length;
                } else {
                    tokenStart = 0;
                    tokenLength = match.Index;
                    AppendInvalidTokenInfo(propertyName, invalidTokens, tokenStart, tokenLength);
                    tokenStart = match.Index + match.Length;
                }

                for (int i = 1; i < matches.Count; i++) {
                    match = matches[i];

                    if (invalidTokens.Length > 0)
                        invalidTokens.Append(", ");

                    tokenLength = match.Index - tokenStart;
                    AppendInvalidTokenInfo(propertyName, invalidTokens, tokenStart, tokenLength);

                    tokenStart = match.Index + match.Length;
                }

                // We might have one last invalid token in the end.
                if (tokenStart != propertyName.Length) {
                    tokenLength = propertyName.Length - tokenStart;

                    if (invalidTokens.Length > 0)
                        invalidTokens.Append(", ");
                    AppendInvalidTokenInfo(propertyName, invalidTokens, tokenStart, tokenLength);
                }

                ErrorHelper.RaiseInvalidPropertyCharactersError(propertyName, invalidTokens.ToString(), debugInfo);
            }
        }

        private void CheckForIllegalPropertyName(string propertyName, DebugInfo debugInfo) {
            for (int i = 0; i < ILLEGAL_PROPERTIES.Length; i++) {
                if (propertyName.Equals(ILLEGAL_PROPERTIES[i], StringComparison.CurrentCultureIgnoreCase)) {
                    ErrorHelper.RaisePropertyExistsError(propertyName, debugInfo);
                    break;
                }
            }
        }

        private static void AppendInvalidTokenInfo(string propertyName, StringBuilder invalidTokens, int position, int length) {
            invalidTokens.Append("{token:'");
            invalidTokens.Append(propertyName.Substring(position, length));
            invalidTokens.Append("', position:");
            invalidTokens.Append(position);
            invalidTokens.Append('}');
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
        /// Raises the invalid property error.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseInvalidPropertyCharactersError(String propertyName, String invalidChars, DebugInfo debugInfo) {
            Error.CompileError.Raise<Object>(
                "Property '" + propertyName + "' contains unsupported characters and is not valid (" + invalidChars + ").",
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="debugInfo">The debug info.</param>
        internal static void RaiseInvalidEditableFlagForMetadata(String name, DebugInfo debugInfo) {
            Error.CompileError.Raise<Object>(
                "Cannot set metadata property " + name + " as editable. The ending '$' should be removed.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }
    }
}
