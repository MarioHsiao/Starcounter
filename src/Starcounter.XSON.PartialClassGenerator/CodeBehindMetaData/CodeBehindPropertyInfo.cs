namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// 
    /// </summary>
    public class CodeBehindFieldOrPropertyInfo {
        /// <summary>
        /// The name of the property declared in code-behind.
        /// </summary>
        public string Name;

        /// <summary>
        /// The fullname of the returntype for the property.
        /// </summary>
        public string TypeName;

        /// <summary>
        /// True if the info points to a property.
        /// </summary>
        public bool IsProperty;
    }
}
