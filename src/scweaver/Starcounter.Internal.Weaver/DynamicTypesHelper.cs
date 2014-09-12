﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sc.Server.Weaver.Schema;

namespace Starcounter.Internal.Weaver {
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;
    using PostSharp;

    /// <summary>
    /// Provides weaver helper methods for dynamic types.
    /// </summary>
    internal static class DynamicTypesHelper {
        /// <summary>
        /// Validates the given attribute to see if custom attributes are
        /// correctly used.
        /// </summary>
        /// <param name="attribute"></param>
        public static void ValidateDatabaseAttribute(DatabaseAttribute attribute) {
            if (attribute.IsTypeReference) {
                ValidateTypeReference(attribute);
            }

            if (attribute.IsInheritsReference) {
                ValidateInheritsReference(attribute);
            }
        }

        static void ValidateTypeReference(DatabaseAttribute attribute) {
            var referencedType = attribute.AttributeType as DatabaseClass;
            if (referencedType == null) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDTYPEREFERENCE,
                    string.Format("Attribute {0}.{1} is not a reference to a database class",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }

            if (attribute.IsTransient) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDTYPEREFERENCE,
                    string.Format("Attribute {0}.{1} is marked transient.",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }

            var other = attribute.DeclaringClass.FindAttributeInAncestors((candidate) => {
                return candidate != attribute && candidate.IsTypeReference;
            });

            if (other != null) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDTYPEREFERENCE,
                    string.Format("Attribute {0}.{1} is marked a type; {2}.{3} is too.",
                    attribute.DeclaringClass.Name,
                    attribute.Name,
                    other.DeclaringClass.Name,
                    other.Name
                    ));
            }

            if (attribute.IsInheritsReference) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDTYPEREFERENCE,
                    string.Format("Attribute {0}.{1} is marked a type; it can not also be marked [Inherits]",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }
        }

        static void ValidateInheritsReference(DatabaseAttribute attribute) {
            var referencedType = attribute.AttributeType as DatabaseClass;
            if (referencedType == null) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDINHERITSREFERENCE,
                    string.Format("Attribute {0}.{1} is not a reference to a database class",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }

            if (attribute.IsTransient) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDINHERITSREFERENCE,
                    string.Format("Attribute {0}.{1} is marked transient.",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }

            var other = attribute.DeclaringClass.FindAttributeInAncestors((candidate) => {
                return candidate != attribute && candidate.IsInheritsReference;
            });

            if (other != null) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDINHERITSREFERENCE,
                    string.Format("Attribute {0}.{1} is marked [Inherits]; {2}.{3} is too.",
                    attribute.DeclaringClass.Name,
                    attribute.Name,
                    other.DeclaringClass.Name,
                    other.Name
                    ));
            }

            if (attribute.IsTypeReference) {
                ScMessageSource.WriteError(
                    MessageLocation.Unknown,
                    Error.SCERRINVALIDTYPEREFERENCE,
                    string.Format("Attribute {0}.{1} is marked [Inherits]; it can not also be marked [Type]",
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }
        }
    }
}