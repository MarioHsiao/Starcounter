using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.XSON.JsonByExample {
    /// <summary>
    /// 
    /// </summary>
    internal class JsonByExampleParser : JsonParser {
        private ITemplateFactory factory;
        private string originFilename;

        private Template parent;
        private Stack<Template> parents;

        internal JsonByExampleParser(string json, string originFilename, ITemplateFactory factory)
            : base(json) {
            this.factory = factory;
            this.originFilename = originFilename;
            this.parents = new Stack<Template>();
        }

        public Template Parse() {
            this.Walk();
            return parent;
        }

        internal override void OnStartObject(string name, string dotnetName, bool isMetadata, bool isEditable) {
            Template template;

            if (isMetadata) {
                if (string.IsNullOrEmpty(dotnetName)) {
                    template = factory.GetMetaTemplate(parent, GetSourceInfo());
                } else {
                    template = factory.GetMetaTemplate(parent, dotnetName, GetSourceInfo());
                }
            } else {
                template = factory.AddObject(parent, name, dotnetName, GetSourceInfo());
            }

            if (parent != null)
                parents.Push(parent);
            parent = template;

            base.OnStartObject(name, dotnetName, isMetadata, isEditable);
        }

        internal override void OnEndObject(string name) {
            if (parents.Count > 0)
                parent = parents.Pop();

            base.OnEndObject(name);
        }

        internal override void OnStartArray(string name, string dotnetName, bool isMetadata, bool isEditable) {
            if (isMetadata)
                ThrowWrongMetadataType("array", GetSourceInfo());

            Template array = factory.AddArray(parent, name, dotnetName, GetSourceInfo());

            if (isEditable)
                array.Editable = true;

            if (parent != null)
                parents.Push(parent);
            parent = array;

            base.OnStartArray(name, dotnetName, isMetadata, isEditable);
        }

        internal override void OnEndArray(string name) {
            if (parents.Count > 0)
                parent = parents.Pop();

            base.OnEndArray(name);
        }

        internal override void OnBoolean(string name, string dotnetName, bool isMetadata, bool isEditable, bool value) {
            if (isMetadata)
                ThrowWrongMetadataType("bool", GetSourceInfo());

            Template template = factory.AddBoolean(parent, name, dotnetName, value, GetSourceInfo());
            if (isEditable)
                template.Editable = true;

            if (parent == null)
                parent = template;

            base.OnBoolean(name, dotnetName, isMetadata, isEditable, value);
        }

        internal override void OnFloat(string name, string dotnetName, bool isMetadata, bool isEditable, decimal value) {
            if (isMetadata)
                ThrowWrongMetadataType("float", GetSourceInfo());

            Template template = factory.AddDecimal(parent, name, dotnetName, value, GetSourceInfo());
            if (isEditable)
                template.Editable = true;

            if (parent == null)
                parent = template;

            base.OnFloat(name, dotnetName, isMetadata, isEditable, value);
        }

        internal override void OnInteger(string name, string dotnetName, bool isMetadata, bool isEditable, long value) {
            if (isMetadata)
                ThrowWrongMetadataType("int", GetSourceInfo());

            Template template = factory.AddInteger(parent, name, dotnetName, value, GetSourceInfo());
            if (isEditable)
                template.Editable = true;

            if (parent == null)
                parent = template;

            base.OnInteger(name, dotnetName, isMetadata, isEditable, value);
        }

        internal override void OnString(string name, string dotnetName, bool isMetadata, bool isEditable, string value) {
            if (isMetadata)
                ThrowWrongMetadataType("string", GetSourceInfo());

            Template template = factory.AddString(parent, name, dotnetName, value, GetSourceInfo());
            if (isEditable)
                template.Editable = true;

            if (parent == null)
                parent = template;

            base.OnString(name, dotnetName, isMetadata, isEditable, value);
        }
        
        private ISourceInfo GetSourceInfo() {
            return new SourceInfo() {
                Filename = originFilename,
                Line = reader.LineNumber,
                Column = reader.LinePosition
            };
        }

        private void ThrowWrongMetadataType(string type, ISourceInfo sourceInfo) {
            throw new TemplateFactoryException("Wrong type for metadata, expected 'object' but was '" + type + "'.", 
                                               sourceInfo);
        }
    }
}
