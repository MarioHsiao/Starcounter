using Starcounter.Templates;

namespace Starcounter.XSON.JsonPatch {
    public struct JsonProperty {
        public readonly Json Json;
        public readonly TValue Property;

        public JsonProperty(Json json, TValue property) {
            Json = json;
            Property = property;
        }
    }
}
