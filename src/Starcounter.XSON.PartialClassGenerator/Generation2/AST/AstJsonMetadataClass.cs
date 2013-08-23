
using Starcounter.XSON.Metadata;
using System;
namespace Starcounter.Internal.MsBuild.Codegen {


    public class AstJsonMetadataClass : AstMetadataClass {
        public AstJsonMetadataClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        public override string ClassStemIdentifier {
            get {
                if (MatchedClass != null) {
#if DEBUG
                    if (MatchedClass.ClassName.Contains("<"))
                        throw new Exception();
                    if (MatchedClass.ClassName.Contains("."))
                        throw new Exception();
#endif
                    return "CodeBehind" + MatchedClass.ClassName;
                }
                var acn = ((AstJsonClass)NValueClass);
#if DEBUG
                if (acn.ClassStemIdentifier.Contains("<"))
                    throw new Exception();
#endif
                //                if (NValueClass.NTemplateClass.Template == Generator.DefaultObjTemplate) {
                //                    return HelperFunctions.GetClassStemIdentifier(NValueClass.NTemplateClass.Template.GetType());
                //                }
                return base.ClassStemIdentifier;
                //return "Metadata";
              //  return "Mupp" + acn.ClassStemIdentifier;
            }
        }
    }
}
