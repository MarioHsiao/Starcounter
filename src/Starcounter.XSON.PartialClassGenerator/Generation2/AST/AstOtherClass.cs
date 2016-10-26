namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Used for classes where a simple class name and inherits name
    /// is sufficient for code generation
    /// </summary>
    public class AstOtherClass : AstClass {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstOtherClass(Gen2DomGenerator gen)
            : base(gen) {
                UseInAliasName = false;
        }
    }
}