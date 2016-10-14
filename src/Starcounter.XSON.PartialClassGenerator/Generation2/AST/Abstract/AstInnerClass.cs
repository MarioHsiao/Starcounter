namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// There are two types of Json inner classes that are used
    /// by the code generator. One is the base or derived
    /// template classes (AstTemplateClass) and one is the base
    /// or dervied meta data bases (AstMetadataClass)
    /// </summary>
    public abstract class AstInnerClass : AstClass {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="generator">The dom generator instance</param>
        public AstInnerClass(Gen2DomGenerator generator)
            : base(generator) {
        }

        private AstInstanceClass _NValueClass;

        /// <summary>
        /// The corresponding Json instance type of this 
        /// metadata class. I.e. if this is a JsonMetadata class,
        /// the InstanceClass is a Json class.
        /// </summary>
        public AstInstanceClass NValueClass {
            get {
                return _NValueClass;
            }
            set { _NValueClass = value; }
        }
        
        public AstProperty NValueProperty;

        public bool IsCodegenerated = false;
    }
}
