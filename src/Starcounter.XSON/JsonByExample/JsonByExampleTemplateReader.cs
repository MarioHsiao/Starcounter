using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.XSON.JSONByExample {
    /// <summary>
    /// 
    /// </summary>
    public class JsonByExampleTemplateReader : IXsonTemplateMarkupReader {
        public Template CreateTemplate(string markup, string source, ITemplateFactory factory) {
            return ReadJson(markup, source, factory);
        }

        private Template ReadJson(string markup, string source, ITemplateFactory factory) {
            bool isMetadata = false;
            bool setEditable = false;
            Template parent = null;
            Template newTemplate = null;
            Stack<Template> templates = new Stack<Template>();
            string propertyName = null;
            string legalName = null;

            var reader = CreateReader(markup);
            
            while (reader.Read()) {
                newTemplate = null;
                switch (reader.TokenType) {
                    case JsonToken.PropertyName:
                        propertyName = (string)reader.Value;
                        legalName = InspectPropertyName(propertyName, out isMetadata, out setEditable);
                        break;
                    case JsonToken.StartObject:
                        if (isMetadata) {
                            if (string.IsNullOrEmpty(legalName))
                                newTemplate = factory.GetMetaTemplate(parent, GetSourceInfo(reader, source));
                            else
                                newTemplate = factory.GetMetaTemplate(parent, legalName, GetSourceInfo(reader, source));
                        } else {
                            newTemplate = factory.AddObject(parent, propertyName, legalName, GetSourceInfo(reader, source));
                        }

                        if (parent != null)
                            templates.Push(parent);
                        parent = newTemplate;
                        break;
                    case JsonToken.EndObject:
                        if (templates.Count > 0)
                            parent = templates.Pop();
                        break;
                    case JsonToken.StartArray:
                        newTemplate = factory.AddArray(parent, propertyName, legalName, GetSourceInfo(reader, source));

                        if (parent != null)
                            templates.Push(parent);
                        parent = newTemplate;
                        break;
                    case JsonToken.EndArray:
                        if (templates.Count > 0)
                            parent = templates.Pop();
                        break;

                    case JsonToken.Boolean:
                        newTemplate = factory.AddBoolean(parent, propertyName, legalName, (bool)reader.Value, GetSourceInfo(reader, source));
                        break;
                    case JsonToken.Date:
                        newTemplate = factory.AddString(parent, propertyName, legalName, reader.Value.ToString(), GetSourceInfo(reader, source));
                        break;
                    case JsonToken.Float:
                        newTemplate = factory.AddDecimal(parent, propertyName, legalName, (decimal)reader.Value, GetSourceInfo(reader, source));
                        break;
                    case JsonToken.Integer:
                        newTemplate = factory.AddInteger(parent, propertyName, legalName, (long)reader.Value, GetSourceInfo(reader, source));
                        break;
                    case JsonToken.String:
                        newTemplate = factory.AddString(parent, propertyName, legalName, (string)reader.Value, GetSourceInfo(reader, source));
                        break;

                    case JsonToken.Comment:
                        throw new NotImplementedException();
                        //break;

                    case JsonToken.Null:
                        throw new NotSupportedException("Null is currently not supported in Json-by-example");
                        //break;

                    case JsonToken.Bytes:
                        throw new NotImplementedException();
                        //break;
                    case JsonToken.None:
                        throw new NotImplementedException();
                        //break;
                    case JsonToken.StartConstructor:
                        throw new NotImplementedException();
                        //break;
                    case JsonToken.EndConstructor:
                        throw new NotImplementedException();
                        //break;
                    case JsonToken.Raw:
                        throw new NotImplementedException();
                        //break;

                    case JsonToken.Undefined:
                        throw new NotImplementedException();
                        //break;
                }

                if (setEditable && newTemplate != null)
                    factory.SetEditableProperty(newTemplate, true, GetSourceInfo(reader, source));
            }

            if (templates.Count > 0)
                throw new Exception("1");

            if (parent == null)
                parent = newTemplate;

            return parent;
        }

        private JsonTextReader CreateReader(string markup) {
            var reader = new JsonTextReader(new StringReader(markup));

            reader.DateParseHandling = DateParseHandling.None;
            reader.FloatParseHandling = FloatParseHandling.Decimal;

            return reader;
        }

        private string InspectPropertyName(string propertyName, out bool isMetadata, out bool setEditable) {
            string legalName = "";
            isMetadata = propertyName.StartsWith("$");
            setEditable = propertyName.EndsWith("$");

            legalName = propertyName;
            if (isMetadata)
                legalName = legalName.Substring(1);

            if (setEditable && legalName.Length > 0)
                legalName = legalName.Substring(0, propertyName.Length - 1);

            return legalName;
        }

        private ISourceInfo GetSourceInfo(JsonTextReader reader, string origin) {
            return new SourceInfo() { 
                Line = reader.LineNumber,
                Column = reader.LinePosition,
                Filename = origin
            };
        }
    
        ///// <summary>
        ///// Compile markup.
        ///// </summary>
        ///// <typeparam name="TJson"></typeparam>
        ///// <typeparam name="TTemplate"></typeparam>
        ///// <param name="markup"></param>
        ///// <param name="origin"></param>
        ///// <returns></returns>
        //public TTemplate CompileMarkup<TJson,TTemplate>(string markup, string origin)
        //    where TJson : Json, new()
        //    where TTemplate : TValue {
        //        return CreateFromJs<TJson, TTemplate>(markup, origin, false);
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="source">The source.</param>
        ///// <param name="sourceReference">The source reference.</param>
        ///// <param name="ignoreNonDesignTimeAssigments">if set to <c>true</c> [ignore non design time assigments].</param>
        ///// <returns>TObj.</returns>
        //internal static TTemplate CreateFromJs<TJson, TTemplate>(string source, string sourceReference, bool ignoreNonDesignTimeAssigments)
        //    where TJson : Json, new()
        //    where TTemplate : TValue {
        //    TTemplate appTemplate;

        //    ITemplateFactory factory = new TAppFactory<TJson, TTemplate>();
        //    int skip = 0;
        //    if (!ignoreNonDesignTimeAssigments) {
        //        source = "(" + source + ")";
        //        skip++;
        //    }
        //    appTemplate = (TTemplate)Materializer.BuiltTemplate(source,
        //                                                   sourceReference,
        //                                                   skip,
        //                                                   factory,
        //                                                   ignoreNonDesignTimeAssigments
        //                                            );

        //   VerifyTemplates(appTemplate);
        //    return appTemplate;
        //}

        ///// <summary>
        ///// Verifies the templates.
        ///// </summary>
        ///// <param name="containerTemplate">The parent template.</param>
        //private static void VerifyTemplates(Template template) {
        //    CompilerOrigin co;
        //    TContainer container;

        //    if (template == null) return;

        //    if (template is ReplaceableTemplate) {
        //        co = template.SourceInfo;
        //        Starcounter.Internal.JsonTemplate.Error.CompileError.Raise<object>(
        //                    "Metadata but no field for '" + template.TemplateName + "' found",
        //                    new Tuple<int, int>(co.LineNo, co.ColNo),
        //                    co.FileName);
        //    }

        //    container = template as TContainer;
        //    if (container != null) {
        //        foreach (Template child in container.Children) {
        //            VerifyTemplates(child);
        //        }
        //    }
        //}
    }
}
