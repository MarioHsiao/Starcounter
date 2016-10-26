using System;
using System.Collections.Generic;
using Starcounter.Templates;
using Starcounter.XSON.Templates.Factory;

namespace Starcounter.XSON.Templates.Factory {
    /// <summary>
    /// A temporary template used to hold values until the correct
    /// template can be created. If for example metadata for a field
    /// is specified before the actual field in the json file, a temporary
    /// ReplaceableTemplate will be created, and later the values will be
    /// copied to the right template.
    /// </summary>
    public class ReplaceableTemplate : Template {
        private Dictionary<string, string> values;

        public override bool IsPrimitive {
            get { throw new NotImplementedException(); }
        }

        public override Type MetadataType {
            get { throw new NotImplementedException(); }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public ReplaceableTemplate() {
            values = new Dictionary<string, string>();
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool HasInstanceValueOnClient {
            get { return false; }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetValue(string name, string value) {
            values[name] = value;
        }
        
        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Unknown; }
        }

        /// <summary>
        /// Copies the replaceable template values.
        /// </summary>
        /// <param name="rt">The rt.</param>
        /// <param name="newTemplate">The new template.</param>
        public void CopyValuesTo(Template template) {
            MetaTemplate tm = new MetaTemplate(template, this.CodegenInfo.SourceInfo);
            foreach (KeyValuePair<string, string> value in values) {
                tm.Set(value.Key, value.Value);
            }
        }
    }
}
