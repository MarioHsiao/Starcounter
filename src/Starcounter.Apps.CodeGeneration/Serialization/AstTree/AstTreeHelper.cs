using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal static class AstTreeHelper {
        internal static string GetAppClassName(AppTemplate template) {
            ArrProperty listing;
            string name;

            name = template.ClassName;
            if (name == null) {
                name = template.Name;
                if (name == null) {
                    listing = template.Parent as ArrProperty;
                    if (listing != null)
                        name = listing.Name;
                    else
                        throw new Exception("Anonymous appclasses not supported for deserialization.");
                }
                name += "App";
            }

            return name;
        }

        internal static string GetFullAppClassName(AppTemplate template) {
            AppTemplate parentAppTemplate;
            IParentTemplate parent;
            ArrProperty lp;
            
            // If this app is an inner innerclass (relative the rootclass) we need to add all parent app names.
            string fullName = null;
            parent = template.Parent;
            if (parent != null) {
                lp = parent as ArrProperty;
                if (lp != null)
                    parent = parent.Parent;
                parentAppTemplate = parent as AppTemplate;
                parent = parent.Parent;
                lp = parent as ArrProperty;
                if (lp != null)
                    parent = parent.Parent;

                if (parent != null) {
                    fullName = GetFullAppClassName(parentAppTemplate);
                }
            }

            if (fullName != null)
                return fullName + '.' + GetAppClassName(template);
            return GetAppClassName(template);
        }

        internal static AstJsonSerializerClass GetSerializerClass(AstNode child) {
            AstNode node;
            AstJsonSerializerClass jsClass;

            node = child.Parent;
            jsClass = null;
            while (node != null) {
                jsClass = node as AstJsonSerializerClass;
                if (jsClass != null)
                    break;
                node = node.Parent;
            }
            return jsClass;
        }
    }
}
