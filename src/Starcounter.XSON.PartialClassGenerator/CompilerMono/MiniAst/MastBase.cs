

using System.Collections.Generic;

namespace Starcounter.Internal.PartialClassGenerator {


    /// <summary>
    /// MastClassDeclaration:
    ///     ClassNameAndGenerics [COLON InheritsList] OPENBRACE
    ///     
    /// ClassNameAndGenerics: Path [ LT ClassNameAndGenerics GT ]
    ///
    /// Path : A-Z 0-9 DOT
    /// 
    /// InheritsList: ClassNameAndGenerics [COMMA InheritsList]
    /// </summary>
    public abstract class MastBase : IReadOnlyTree {

        internal MastBase _Parent;
        internal List<MastBase> _Children = new List<MastBase>();

        public IReadOnlyTree Parent {
            get { return _Parent; }
        }

        public System.Collections.Generic.IReadOnlyList<IReadOnlyTree> Children {
            get { return _Children; }
        }
    }
}
