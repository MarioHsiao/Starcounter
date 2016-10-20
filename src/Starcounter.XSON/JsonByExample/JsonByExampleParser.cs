using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.XSON.JsonByExample {
    /// <summary>
    /// Class that parses json and converts it to a TypedJSON template structure.
    /// </summary>
    internal class JsonByExampleParser : JsonParser {
        private class PropertyInfo {
            internal bool isMetadata;
            internal bool isEditable;
            internal string dotnetName;
        }

        private static ITemplateFactory factory = new TemplateFactory();

        private Stack<Template> parents;
        private string originFilename;
        private Template currentParent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="originFilename"></param>
        internal JsonByExampleParser(string json, string originFilename)
            : base(json) {
            this.parents = null;
            this.originFilename = originFilename;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Template CreateTemplate() {
            try {
                this.Parse();
            } catch (Exception ex) {
                throw new TemplateFactoryException("JsonByExample parsing failed: " + ex.Message, GetSourceInfo());
            }

            factory.Verify(currentParent);
            return currentParent;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        protected override void BeginObject(string name) {
            Template template;
            PropertyInfo pi = ProcessName(name);
            
            if (pi.isMetadata) {
                if (string.IsNullOrEmpty(pi.dotnetName)) {
                    template = factory.GetMetaTemplate(currentParent, GetSourceInfo());
                } else {
                    template = factory.GetMetaTemplate(currentParent, pi.dotnetName, GetSourceInfo());
                }
            } else {
                template = factory.AddObject(currentParent, name, pi.dotnetName, GetSourceInfo());
            }

            if (currentParent != null) {
                if (parents == null)
                    parents = new Stack<Template>();
                parents.Push(currentParent);
            }
            currentParent = template;

            base.BeginObject(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        protected override void EndObject(string name) {
            if (parents?.Count > 0)
                currentParent = parents.Pop();

            base.EndObject(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        protected override void BeginArray(string name) {
            PropertyInfo pi = ProcessName(name);

            if (pi.isMetadata)
                ThrowWrongMetadataType("array", GetSourceInfo());

            Template array = factory.AddArray(currentParent, name, pi.dotnetName, GetSourceInfo());
            if (array != null)
                array.Editable = pi.isEditable;

            if (currentParent != null) {
                if (parents == null)
                    parents = new Stack<Template>();
                parents.Push(currentParent);
            }
            currentParent = array;

            base.BeginArray(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        protected override void EndArray(string name) {
            var arr = currentParent as TObjArr;
            if (arr != null && arr.ElementType == null) {
                // Creating a default empty object. Will be treated as an 
                // untyped array anyway so all values will be allowed.
                arr.ElementType = new TObject();
            }
            
            if (parents?.Count > 0)
                currentParent = parents.Pop();
            
            base.EndArray(name);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void Property(string name, bool value) {
            PropertyInfo pi = ProcessName(name);

            if (pi.isMetadata)
                ThrowWrongMetadataType("bool", GetSourceInfo());

            Template template = factory.AddBoolean(currentParent, name, pi.dotnetName, value, GetSourceInfo());
            if (template != null)
                template.Editable = pi.isEditable;

            if (currentParent == null)
                currentParent = template;

            base.Property(name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void Property(string name, decimal value) {
            PropertyInfo pi = ProcessName(name);

            if (pi.isMetadata)
                ThrowWrongMetadataType("float", GetSourceInfo());

            Template template = factory.AddDecimal(currentParent, name, pi.dotnetName, value, GetSourceInfo());
            if (template != null)
                template.Editable = pi.isEditable;

            if (currentParent == null)
                currentParent = template;

            base.Property(name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void Property(string name, long value) {
            PropertyInfo pi = ProcessName(name);

            if (pi.isMetadata)
                ThrowWrongMetadataType("int", GetSourceInfo());

            Template template = factory.AddInteger(currentParent, name, pi.dotnetName, value, GetSourceInfo());
            if (template != null)
                template.Editable = pi.isEditable;

            if (currentParent == null)
                currentParent = template;

            base.Property(name, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected override void Property(string name, string value) {
            PropertyInfo pi = ProcessName(name);

            if (pi.isMetadata)
                ThrowWrongMetadataType("string", GetSourceInfo());

            Template template = factory.AddString(currentParent, name, pi.dotnetName, value, GetSourceInfo());
            if (template != null)
                template.Editable = pi.isEditable;

            if (currentParent == null)
                currentParent = template;

            base.Property(name, value);
        }

        protected override void Null(string name) {
            PropertyInfo pi = ProcessName(name);

            // Currently we only allow setting the 'Bind' property to null inside Json-by-example. 
            // All other cases will lead to an exception.
            if ("bind".Equals(name, StringComparison.InvariantCultureIgnoreCase)
                && currentParent is MetaTemplate) {
                factory.AddString(currentParent, name, pi.dotnetName, null, GetSourceInfo());
                base.Null(name);
                return;
            }

            throw new NotSupportedException("Null is currently not supported in Json-by-example");
        }

        /// <summary>
        /// Returns an instance holding information about where a specific token is processed.
        /// </summary>
        /// <returns></returns>
        private ISourceInfo GetSourceInfo() {
            return new SourceInfo() {
                Filename = originFilename,
                Line = reader.LineNumber,
                Column = reader.LinePosition
            };
        }

        /// <summary>
        /// Since JsonByExample have some specific syntax for allowing metadata to be specified,
        /// we check the property-name in case the value should be treated as a metadata-object.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private PropertyInfo ProcessName(string propertyName) {
            var pi = new PropertyInfo();

            if (string.IsNullOrEmpty(propertyName)) {
                pi.isMetadata = false;
                pi.isEditable = false;
                pi.dotnetName = null;
                return pi;
            }

            string legalName = "";
            pi.isMetadata = propertyName.StartsWith("$");
            pi.isEditable = propertyName.EndsWith("$");

            legalName = propertyName;
            if (pi.isMetadata)
                legalName = legalName.Substring(1);

            if (pi.isEditable && legalName.Length > 0)
                legalName = legalName.Substring(0, propertyName.Length - 1);

            pi.dotnetName = legalName;
            return pi;
        }

        private void ThrowWrongMetadataType(string type, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException("Wrong type for metadata, expected 'object' but was '" + type + "'.", 
                                               sourceInfo);
        }
    }
}
