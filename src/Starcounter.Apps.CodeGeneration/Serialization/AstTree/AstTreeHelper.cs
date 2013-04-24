using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal static class AstTreeHelper {
        internal static string GetAppClassName(TObj template) {
            TObjArr listing;
            string name;

            name = template.ClassName;
            if (name == null) {
                name = template.TemplateName;
                if (name == null) {
                    listing = template.Parent as TObjArr;
                    if (listing != null)
                        name = listing.TemplateName;
                    else
                        throw new Exception("Anonymous appclasses not supported for deserialization.");
                }
                name += "App";
            }

            return name;
        }

        internal static string GetFullAppClassName(TObj template) {
            TObj parentTApp;
            TContainer parent;
            TObjArr lp;
            
            // If this app is an inner innerclass (relative the rootclass) we need to add all parent app names.
            string fullName = null;
            parent = template.Parent;
            if (parent != null) {
                lp = parent as TObjArr;
                if (lp != null)
                    parent = parent.Parent;
                parentTApp = parent as TObj;
                parent = parent.Parent;
                lp = parent as TObjArr;
                if (lp != null)
                    parent = parent.Parent;

                if (parent != null) {
                    fullName = GetFullAppClassName(parentTApp);
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
