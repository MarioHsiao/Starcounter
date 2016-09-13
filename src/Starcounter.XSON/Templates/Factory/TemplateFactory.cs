// ***********************************************************************
// <copyright file="TAppFactory.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;
using System.Text.RegularExpressions;
using System.Text;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    /// <summary>
    /// The template factory is intended for template parsers as a clean
    /// interface used to built Starcounter controller templates.
    /// It is used as a singleton.
    /// </summary>
    public class TemplateFactory : ITemplateFactory {
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
            string name;
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
                        rt.CopyValuesTo(newTemplate);
                        objParent.Properties.Replace(newTemplate);
                    } else {
                        FactoryExceptionHelper.RaisePropertyExistsError(name, debugInfo);
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
                        rt.CopyValuesTo(newTemplate);
                    }
                }
                
                arrParent.ElementType = (TValue)newTemplate;
                newTemplate.Parent = arrParent;
            } 
            return newTemplate;
        }
        
        /// <summary>
        /// Gets the meta template.
        /// </summary>
        /// <param name="templ">The templ.</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.GetMetaTemplate(Template template, ISourceInfo sourceInfo) {
            return new MetaTemplate((Template)template, sourceInfo);
        }

        /// <summary>
        /// Gets the meta template for property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="debugInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.GetMetaTemplate(Template parent, string name, ISourceInfo sourceInfo) {
            if (parent == null)
                return null;

            var appTemplate = parent as TObject;

            var propertyName = name;
            if (name.EndsWith("$")) {
                propertyName = name.Substring(0, name.Length - 1);
            }

            var t = appTemplate.Properties.GetTemplateByPropertyName(propertyName);
            if (t == null) {
                // The template is not created yet. This can be because the metadata 
                // is specified before the actual field in the json file.
                // We create a dummy template that will be replaced later.
                // If this dummy template is not replaced later, an exception
                // will be raised.
                t = new ReplaceableTemplate() { TemplateName = name };
                t.CodegenInfo.SourceInfo = sourceInfo;
                appTemplate.Properties.Add(t);
            }
            return new MetaTemplate(t, sourceInfo);
        }

        /// <summary>
        /// Adds the string property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        ///
        Template ITemplateFactory.AddString(Template parent,
                                           string name,
                                           string dotNetName,
                                           string value,
                                           ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TString() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                if (newTemplate is TString)
                    ((TString)newTemplate).DefaultValue = value;

                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
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
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddInteger(Template parent,
                                             string name,
                                             string dotNetName,
                                             long value,
                                             ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);


            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TLong() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                if (newTemplate is TLong)
                    ((TLong)newTemplate).DefaultValue = value;
                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            } 
        }

        /// <summary>
        /// Adds the decimal property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddDecimal(Template parent,
                                             string name,
                                             string dotNetName,
                                             decimal value,
                                             ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TDecimal() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                if (newTemplate is TDecimal)
                    ((TDecimal)newTemplate).DefaultValue = value;
                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            }
        }

        /// <summary>
        /// Adds the double property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">The value.</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddDouble(Template parent,
                                          string name,
                                          string dotNetName,
                                          double value,
                                          ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);

            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TDouble() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                if (newTemplate is TDouble)
                    ((TDouble)newTemplate).DefaultValue = value;

                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            }
        }

        /// <summary>
        /// Adds the boolean property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddBoolean(Template parent,
                                             string name,
                                             string dotNetName,
                                             bool value,
                                             ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);
            if (parent is MetaTemplate) {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            } else {
                newTemplate = new TBool() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                if (newTemplate is TBool)
                    ((TBool)newTemplate).DefaultValue = value;
                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            }
        }
       
        /// <summary>
        /// Adds the array property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddArray(Template parent,
                                           string name, 
                                           string dotNetName,
                                           ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);

            if (parent is MetaTemplate) {
                FactoryExceptionHelper.RaiseWrongValueForPropertyError(name, "string", "array", sourceInfo);
                return null;
            } else {
                newTemplate = new TArray<Json>() { TemplateName = name };
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            }
        }

        /// <summary>
        /// Adds the app property.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <param name="dotNetName">A legal C# property name (with non C# characters, such as $, stripped out)</param>
        /// <param name="sourceInfo">The debug info.</param>
        /// <returns>System.Object.</returns>
        Template ITemplateFactory.AddObject(Template parent, string name, string dotNetName, ISourceInfo sourceInfo) {
            Template newTemplate;

            VerifyPropertyName(dotNetName, sourceInfo);

            if (parent is MetaTemplate) {
                FactoryExceptionHelper.RaiseWrongValueForPropertyError(name, "string", "object", sourceInfo);
                return null;
            } else {
                newTemplate = new TObject();
                if (parent != null) {
                    newTemplate.TemplateName = name;
                    newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, parent, sourceInfo);
                }
                newTemplate.CodegenInfo.SourceInfo = sourceInfo;
                return newTemplate;
            }
        }
        
        private void VerifyPropertyName(string propertyName, ISourceInfo sourceInfo) {
            if (propertyName == null)
                return;

            CheckForInvalidCharactersInPropertyName(propertyName, sourceInfo);
            CheckForIllegalPropertyName(propertyName, sourceInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="sourceInfo"></param>
        private void CheckForInvalidCharactersInPropertyName(string propertyName, ISourceInfo sourceInfo) {
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

                FactoryExceptionHelper.RaiseInvalidPropertyCharactersError(propertyName, invalidTokens.ToString(), sourceInfo);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Verify(Template template) {
            TContainer container;

            if (template == null)
                return;

            if (template is ReplaceableTemplate) {
                FactoryExceptionHelper.ThrowMetadataButNoPropertyException(template.TemplateName, template.CodegenInfo.SourceInfo);
            }

            container = template as TContainer;
            if (container != null) {
                foreach (Template child in container.Children) {
                    Verify(child);
                }
            }
        }

        private void CheckForIllegalPropertyName(string propertyName, ISourceInfo sourceInfo) {
            for (int i = 0; i < ILLEGAL_PROPERTIES.Length; i++) {
                if (propertyName.Equals(ILLEGAL_PROPERTIES[i], StringComparison.CurrentCultureIgnoreCase)) {
                    FactoryExceptionHelper.RaisePropertyExistsError(propertyName, sourceInfo);
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
}
