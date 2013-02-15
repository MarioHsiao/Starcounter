using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal.Application.CodeGeneration.Serialization;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    public class NAppSerializerClass : NBase {

        public NAppSerializerClass(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// 
        /// </summary>
        public NAppClass NAppClass { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetSerializerClassCode() {
            AstNode astTree = AstTreeGenerator.BuildAstTree(NAppClass.Template);
            //astTree.Indentation = 4;
            return astTree.GenerateCsSourceCode();
        }
    }
}
