
namespace Starcounter.Advanced.XSON {
    public enum MissingMemberHandling {
        Error = 0,
        Ignore = 1,
    }

    /// <summary>
    /// Used to specify settings for the jsonserializer.
    /// </summary>
    public class JsonSerializerSettings {
        public JsonSerializerSettings() { }

        /// <summary>
        /// Specifies how unknown members should be handled when encountered during population of data.
        /// </summary>
        public MissingMemberHandling MissingMemberHandling { get; set; }
    }
}