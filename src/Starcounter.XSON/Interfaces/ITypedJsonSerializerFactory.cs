
using Starcounter.Templates;
namespace Starcounter.Advanced.XSON {

    /// <summary>
    /// Provides the ability to inject faster ways to serialize or deserialize
    /// Typed Json objects.
    /// </summary>
    public interface ITypedJsonSerializerFactory {

        /// <summary>
        /// A serializer/deserializer is always associate with a specific
        /// XSON template (i.e. schema)
        /// </summary>
        /// <param name="template">The template associated with the serializer/deserializer to be created</param>
        /// <returns>The serializer/deserializer</returns>
        TypedJsonSerializer CreateTypedJsonSerializer(TObj template);
    }
}
