namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// 
    /// </summary>
    public class CodeBehindTypeAssignmentInfo {
        /// <summary>
        /// The full path of the template that assigned the property 'InstanceType'
        /// in code-behind (excluding 'InstanceType')
        /// </summary>
        public string TemplatePath;

        /// <summary>
        /// The typename of the type being assigned to 'InstanceType'
        /// </summary>
        public string TypeName;
    }
}
