namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// 
    /// </summary>
    public class AstRoot : AstBase {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstRoot(Gen2DomGenerator gen)
            : base(gen) {
        }

       // public string RootJsonClassAliasPrefix;
       // public string RootJsonClassAlias;

        /// <summary>
        /// The app class class node
        /// </summary>
        public AstJsonClass AppClassClassNode;
        public bool AliasesActive = false;

      //  public TObj DefaultObjTemplate;
    }
}
