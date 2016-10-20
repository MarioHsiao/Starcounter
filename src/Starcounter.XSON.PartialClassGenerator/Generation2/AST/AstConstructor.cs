namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Represents a constructor
    /// </summary>
    public class AstConstructor : AstBase {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstConstructor(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "NCONSTRUCTOR";
        }
    }
}
