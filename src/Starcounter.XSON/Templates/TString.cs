// ***********************************************************************
// <copyright file="TString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TString : PrimitiveProperty<string> {
        public override void ProcessInput(Json obj, byte[] rawValue) {
            obj.ProcessInput<string>(this, System.Text.Encoding.UTF8.GetString(rawValue));
        }
        public override Type MetadataType {
            get { return typeof(StringMetadata<Json>); }
        }


        private string _DefaultValue = "";

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public string DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (string)value;
            }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(string); }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="addToChangeLog"></param>
		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			if (UseBinding(parent)) {
				string boundValue = BoundGetter(parent);
				string oldValue = UnboundGetter(parent);

				if ((boundValue == null && oldValue != null) 
					|| (boundValue != null && !boundValue.Equals(oldValue))) {
					UnboundSetter(parent, boundValue);
					if (addToChangeLog)
						parent.Session.UpdateValue(parent, this);
				}
			}	
		}

		internal override string ValueToJsonString(Json parent) {
			string value = Getter(parent);
			if (value != null)
				return '"' + value + '"';
			return "null";
		}
    }
}
