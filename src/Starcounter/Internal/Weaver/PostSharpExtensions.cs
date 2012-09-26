
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Internal.Weaver {
    
    internal static class PostSharpExtensions {
        public static string GetReflectionName(this ITypeSignature type) {
            StringBuilder builder = new StringBuilder();
            type.WriteReflectionName(builder, ReflectionNameOptions.None);
            return builder.ToString();
        }

    }
}