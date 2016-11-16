namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// 
    /// </summary>
    public class CodeBehindAssignmentInfo {
        /// <summary>
        /// The full path to get the template that hada property assigned 
        /// in code-behind (excluding the property itself).
        /// </summary>
        public string TemplatePath;

        /// <summary>
        /// The value of the assignment.
        /// </summary>
        public string Value;
    }
}
