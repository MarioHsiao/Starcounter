using System;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
    [Obsolete("This class have been deprecated and will be removed soon. Please use ITypedJsonSerializer interface instead.")]
    public abstract class TypedJsonSerializer {
        [Obsolete("This field have been deprecated and will be removed soon. Please use the default settings in JsonSerializerSettings class.")]
        public static JsonSerializerSettings DefaultSettings = JsonSerializerSettings.Default;

        [Obsolete("This method should no longer be used and will be removed soon. Use methods in ITypedJsonSerializer interface instead.")]
        public abstract int EstimateSizeBytes(Json obj, JsonSerializerSettings settings = null);
        [Obsolete("This method should no longer be used and will be removed soon. Use methods in ITypedJsonSerializer interface instead.")]
        public abstract int EstimateSizeBytes(Json obj, Template property, JsonSerializerSettings settings = null);
        [Obsolete("This method should no longer be used and will be removed soon. Use methods in ITypedJsonSerializer interface instead.")]
        public abstract int Serialize(Json json, IntPtr dest, int destSize, JsonSerializerSettings settings = null);
        [Obsolete("This method should no longer be used and will be removed soon. Use methods in ITypedJsonSerializer interface instead.")]
        public abstract int Serialize(Json json, Template property, IntPtr dest, int destSize, JsonSerializerSettings settings = null);
        [Obsolete("This method should no longer be used and will be removed soon. Use methods in ITypedJsonSerializer interface instead.")]
        public abstract int Populate(Json json, IntPtr src, int srcSize, JsonSerializerSettings settings = null);
    }
}
