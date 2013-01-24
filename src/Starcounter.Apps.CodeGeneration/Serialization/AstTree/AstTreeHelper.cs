﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;
using Starcounter.Templates.Interfaces;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal static class AstTreeHelper {
        internal static string GetAppClassName(AppTemplate template) {
            ListingProperty listing;
            string name;

            name = template.ClassName;
            if (name == null) {
                name = template.Name;
                if (name == null) {
                    listing = template.Parent as ListingProperty;
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
            ListingProperty lp;
            
            // If this app is an inner innerclass (relative the rootclass) we need to add all parent app names.
            string fullName = null;
            parent = template.Parent;
            if (parent != null) {
                lp = parent as ListingProperty;
                if (lp != null)
                    parent = parent.Parent;
                parentAppTemplate = parent as AppTemplate;
                parent = parent.Parent;
                lp = parent as ListingProperty;
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
    }
}
