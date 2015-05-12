// ***********************************************************************
// <copyright file="TString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Advanced.XSON;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    public class TString : PrimitiveProperty<string> {
        public TString() {
            DefaultValue = "";
        }

        public override Type MetadataType {
            get { return typeof(StringMetadata<Json>); }
        }

        internal override void SetDefaultValue(Json parent) {
            UnboundSetter(parent, DefaultValue);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        internal override Type DefaultInstanceType {
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

        internal override int TemplateTypeId {
            get { return (int)TemplateTypeEnum.String; }
        }
    }
}
