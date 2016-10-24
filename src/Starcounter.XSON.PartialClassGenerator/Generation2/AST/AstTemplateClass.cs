using Starcounter.Templates;

namespace Starcounter.XSON.PartialClassGenerator {
    public class AstTemplateClass : AstInnerClass {
        public AstTemplateClass(Gen2DomGenerator gen)
            : base(gen) {
        }
        
        private Template _Template;

        /// <summary>
        /// The schema template instance that was used to create
        /// this syntax node. I.e. if this is a AstSchema, 
        /// the Template would be a Schema&ltJson&ltobject&gt&gt.
        /// </summary>
        public Template Template {
            get {
                return _Template;
            }
            set {
                _Template = value;
            }
        }
        
//        public override string ClassStemIdentifier {
//            get {
//                return HelperFunctions.GetClassStemIdentifier(Template.GetType());
//            }
//        }
    }
}

