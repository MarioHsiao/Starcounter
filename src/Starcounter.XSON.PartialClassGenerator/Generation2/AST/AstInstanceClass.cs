namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
    /// </summary>
    public class AstInstanceClass : AstClass {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstInstanceClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets or sets the N template class.
        /// </summary>
        /// <value>The N template class.</value>
        public AstTemplateClass NTemplateClass { get; set; }

        /// <summary>
        /// Gets or sets the N template class.
        /// </summary>
        /// <value>The N template class.</value>
        public AstMetadataClass NMetadataClass { get; set; }
    }
}
