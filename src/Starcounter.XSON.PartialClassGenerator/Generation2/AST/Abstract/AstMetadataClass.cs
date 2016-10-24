using System.Collections.Generic;
using Starcounter.Templates;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Class NMetadataClass
    /// </summary>
    public class AstMetadataClass : AstInnerClass {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstMetadataClass(Gen2DomGenerator gen)
            : base(gen) {
        }
        
        /// <summary>
        /// Uppers the first.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>System.String.</returns>
        public static string UpperFirst( string str ) {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        /// <summary>
        /// The instances
        /// </summary>
        public static Dictionary<TObject, AstClass> Instances = new Dictionary<TObject, AstClass>();
    }
}
