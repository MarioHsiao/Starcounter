
using System;
using System.Collections.Generic;
using Starcounter;
using Starcounter.Templates;

namespace Starcounter.Internal
{
    internal class MetaTemplate
    {
        private static List<String> _booleanProperties;
        private static List<String> _stringProperties;

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
        }

        private Starcounter.Templates.Template _template;
        private DebugInfo _debugInfo;

        internal MetaTemplate(Template forTemplate, DebugInfo debugInfo)
        {
            _template = forTemplate;
            _debugInfo = debugInfo;
        }

        public void Set(string name, bool v)
        {
            Property property;
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
                property = _template as Property;
                if (property == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                property.Editable = v;
            }
            else if (upperName == "BOUND")
            {
                property = _template as Property;
                if (property == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                property.Bound = v;
            }
            else
            {
                if (_stringProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "string", "boolean", _debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, _debugInfo);
            }
        }

        public void Set(string name, string v)
        {
            ActionProperty actionTemplate;
            AppTemplate appTemplate;
            Property valueTemplate;
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
                valueTemplate = _template as Property;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                valueTemplate.OnUpdate = v;
            }
            else if (upperName == "CLASS")
            {
                appTemplate = _template as AppTemplate;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                ((AppTemplate)_template).ClassName = v;
            }
            else if (upperName == "RUN")
            {
                actionTemplate = _template as ActionProperty;
                if (actionTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                actionTemplate.OnRun = v;
            }
            else if (upperName == "BIND")
            {
                valueTemplate = _template as Property;
                if (valueTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);
                valueTemplate.Bind = v;
                valueTemplate.Bound = true;
            }
            else if (upperName == "TYPE")
            {
                Property oldProperty = _template as Property;
                if (oldProperty == null || (oldProperty is AppTemplate)) 
                    ErrorHelper.RaiseInvalidTypeConversionError(_debugInfo);

                Property newProperty = GetPropertyFromTypeName(v);
                oldProperty.CopyTo(newProperty);

                AppTemplate parent = (AppTemplate)oldProperty.Parent;
                parent.Properties.Replace(newProperty);
            }
            else if (upperName == "REUSE")
            {
                ErrorHelper.RaiseNotImplementedException(name, _debugInfo);
            }
            else if (upperName == "NAMESPACE")
            {
                appTemplate = _template as AppTemplate;
                if (appTemplate == null) ErrorHelper.RaiseInvalidPropertyError(name, _debugInfo);

                appTemplate.Namespace = v;
            }
            else
            {
                if (_booleanProperties.Contains(upperName))
                    ErrorHelper.RaiseWrongValueForPropertyError(name, "boolean", "string", _debugInfo);
                else
                    ErrorHelper.RaiseUnknownPropertyError(name, _debugInfo);
            }
        }

        private Property GetPropertyFromTypeName(string v)
        {
            Property p = null;
            String nameToUpper = v.ToUpper();
            switch (nameToUpper)
            {
                case "DOUBLE":
                case "FLOAT":
                    p = new DoubleProperty();
                    break;
                case "DECIMAL":
                    p = new DecimalProperty();
                    break;
                case "INT":
                case "INTEGER":
                case "INT32":
                    p = new IntProperty();
                    break;
                case "STRING":
                    p = new StringProperty();
                    break;
                default:
                    ErrorHelper.RaiseUnknownPropertyTypeError(v, _debugInfo);
                    break;
            }

            if (p != null) 
                SetCompilerOrigin(p, _debugInfo);

            return p;
        }

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
    public class AppTemplateFactory : ITemplateFactory
    {
        /// <summary>
        /// Checks if the specified name already exists. If the name exists
        /// and is not used by an ReplaceableTemplate an exception is thrown.
        /// 
        /// In case the existing template is an ReplaceableTemplate all values 
        /// set on it are copied to the new template, and the ReplaceableTemplate is 
        /// replaced with the new template.
        /// 
        /// If no template exists, the new template is added to the parent.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newTemplate"></param>
        /// <param name="parent"></param>
        /// <param name="debugInfo"></param>
        private Template CheckAndAddOrReplaceTemplate(Template newTemplate, 
                                                       AppTemplate parent,
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
                        if (!(newTemplate is Property)) 
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

        private void CopyReplaceableTemplateValues(ReplaceableTemplate rt, Template newTemplate)
        {
            Boolean boolVal;
            CompilerOrigin co;
            String strVal;

            co = rt.CompilerOrigin;
            MetaTemplate tm 
                = new MetaTemplate(newTemplate, new DebugInfo(co.LineNo, co.ColNo, co.FileName));
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

        object ITemplateFactory.GetMetaTemplate(object templ, DebugInfo debugInfo)
        {
            return new MetaTemplate((Template)templ, debugInfo);
        }

        object ITemplateFactory.GetMetaTemplateForProperty(object parent, 
                                                           string name,
                                                           DebugInfo debugInfo)
        {
            var appTemplate = (AppTemplate)parent;
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
            return new MetaTemplate(t, debugInfo);
        }

        object ITemplateFactory.AddStringProperty(object parent,
                                                  string name,
                                                  string value,
                                                  DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            if (parent is MetaTemplate)
            {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            }
            else
            {
                newTemplate = new StringProperty() { Name = name };
                appTemplate = (AppTemplate)parent;

                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
        }

        object ITemplateFactory.AddIntegerProperty(object parent,
                                                   string name,
                                                   int value,
                                                   DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate))
            {
                newTemplate = new IntProperty() { Name = name };
                appTemplate = (AppTemplate)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        object ITemplateFactory.AddDecimalProperty(object parent,
                                                   string name,
                                                   decimal value,
                                                   DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate))
            {
                newTemplate = new DecimalProperty() { Name = name };
                appTemplate = (AppTemplate)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        object ITemplateFactory.AddDoubleProperty(object parent,
                                                  string name,
                                                  double value,
                                                  DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            if (!(parent is MetaTemplate))
            {
                newTemplate = new DoubleProperty() { Name = name };
                appTemplate = (AppTemplate)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
            return null;
        }

        object ITemplateFactory.AddBooleanProperty(object parent,
                                                   string name,
                                                   bool value,
                                                   DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            if (parent is MetaTemplate)
            {
                ((MetaTemplate)parent).Set(name, value);
                return null;
            }
            else
            {
                newTemplate = new BoolProperty() { Name = name };
                appTemplate = (AppTemplate)parent;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
                SetCompilerOrigin(newTemplate, debugInfo);
                return newTemplate;
            }
        }

        void SetCompilerOrigin(Template t, DebugInfo d)
        {
            t.CompilerOrigin.FileName = d.FileName;
            t.CompilerOrigin.LineNo = d.LineNo;
            t.CompilerOrigin.ColNo = d.ColNo;
        }

        object ITemplateFactory.AddEventProperty(object parent,
                                                 string name,
                                                 string value,
                                                 DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

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

            newTemplate = new ActionProperty() { Name = name };
            appTemplate = (AppTemplate)parent;
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        object ITemplateFactory.AddArrayProperty(object parent,
                                                 string name,
                                                 DebugInfo debugInfo)
        {
            AppTemplate appTemplate;
            Template newTemplate;

            newTemplate = new ListingProperty() { Name = name };
            appTemplate = (AppTemplate)parent;
            newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

//        object ITemplateFactory.AddObjectProperty(object parent,
//                                                  string name,
//                                                  DebugInfo debugInfo)
//        {
//            AppTemplate appTemplate;
//            Template newTemplate;
//
//            newTemplate = new ObjectProperty();
//            appTemplate = parent as AppTemplate;
//            if (parent != null)
//            {
//                newTemplate.Name = name;
//                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, appTemplate, debugInfo);
//            }
//            SetCompilerOrigin(newTemplate, debugInfo);
//            return newTemplate;
//        }

        object ITemplateFactory.AddAppProperty(object parent, string name, DebugInfo debugInfo)
        {
            Template newTemplate;

            newTemplate = new AppTemplate();
            if (parent != null)
            {
                newTemplate.Name = name;
                newTemplate = CheckAndAddOrReplaceTemplate(newTemplate, (AppTemplate)parent, debugInfo);
            }
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        object ITemplateFactory.AddAppElement(object array, DebugInfo debugInfo)
        {
            var newTemplate = new AppTemplate(); // The type of the type array (an AppTemplate)
            newTemplate.Parent = (ParentTemplate)array;
            //			newTemplate.Name = "__ArrayType__"; // All children needs an id
            var arr = ((ListingProperty)array);
            arr.App = newTemplate;
            newTemplate.Parent = arr;
            SetCompilerOrigin(newTemplate, debugInfo);
            return newTemplate;
        }

        object ITemplateFactory.AddCargoProperty(object parent, DebugInfo debugInfo)
        {
            throw new NotImplementedException();
        }

        object ITemplateFactory.AddMetaProperty(object template, DebugInfo debugInfo)
        {
            throw new NotImplementedException();
        }

        void ITemplateFactory.SetEditableProperty(object template, bool b, DebugInfo debugInfo)
        {
            ((Template)template).Editable = b;
        }

        void ITemplateFactory.SetBoundProperty(object template, bool b, DebugInfo debugInfo)
        {
            ((Template)template).Bound = b;
        }

        void ITemplateFactory.SetClassProperty(object template,
                                               string className,
                                               DebugInfo debugInfo)
        {
            ((AppTemplate)template).ClassName = className;
        }

        void ITemplateFactory.SetIncludeProperty(object template,
                                                 string className,
                                                 DebugInfo debugInfo)
        {
            ((AppTemplate)template).Include = className;
        }

        void ITemplateFactory.SetNamespaceProperty(object template,
                                                   string namespaceName,
                                                   DebugInfo debugInfo)
        {
            ((AppTemplate)template).Namespace = namespaceName;
        }

        void ITemplateFactory.SetOnUpdateProperty(object template, 
                                                  string functionName, 
                                                  DebugInfo debugInfo)
        {
            ((Template)template).OnUpdate = functionName;
        }

        void ITemplateFactory.SetBindProperty(object template, string path, DebugInfo debugInfo)
        {
            ((Template)template).Bind = path;
        }
    }

    internal static class ErrorHelper
    {
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

        internal static void RaiseInvalidPropertyError(String propertyName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Property '" + propertyName + "' is not valid on this field.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        internal static void RaiseInvalidTypeConversionError(DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Invalid field for Type property. Valid fields are string, int, decimal, double and boolean.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        internal static void RaiseInvalidTypeConversionError(CompilerOrigin co)
        {
            RaiseInvalidTypeConversionError(new DebugInfo(co.LineNo, co.ColNo, co.FileName));
        }

        internal static void RaiseUnknownPropertyError(String propertyName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Unknown property '" + propertyName + "'.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

        internal static void RaiseUnknownPropertyTypeError(String typeName, DebugInfo debugInfo)
        {
            Error.CompileError.Raise<Object>(
                "Unknown type '" + typeName + "'.",
                new Tuple<int, int>(debugInfo.LineNo, debugInfo.ColNo),
                debugInfo.FileName
            );
        }

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
