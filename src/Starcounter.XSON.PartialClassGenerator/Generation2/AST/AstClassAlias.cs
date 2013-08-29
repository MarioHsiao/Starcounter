

namespace Starcounter.Internal.MsBuild.Codegen {

    public class AstClassAlias : AstBase {

        public string Alias;
        public string Specifier;

        public AstClassAlias(Gen2DomGenerator generator)
            :base(generator) {
        }

        public override string ToString() {
            return this.GetType().Name + " " + this.Alias + " " + this.Specifier;
        }
    }
}
