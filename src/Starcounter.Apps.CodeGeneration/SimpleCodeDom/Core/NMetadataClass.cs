

using Starcounter.Templates;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    public class NMetadataClass : NClass {

        public static Dictionary<Template, NMetadataClass> Classes = new Dictionary<Template, NMetadataClass>();

        public static NMetadataClass Find( Template template ) {
            template = NTemplateClass.GetPrototype(template);
            return NMetadataClass.Classes[template];
        }

        static NMetadataClass() {
            Classes[NTemplateClass.StringProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.StringProperty] };
            Classes[NTemplateClass.IntProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.IntProperty] };
            Classes[NTemplateClass.DecimalProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DecimalProperty] };
            Classes[NTemplateClass.DoubleProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DoubleProperty] };
            Classes[NTemplateClass.BoolProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.BoolProperty] };
            Classes[NTemplateClass.ActionProperty] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.ActionProperty] };
            Classes[NTemplateClass.AppTemplate] = new NMetadataClass { NTemplateClass = NTemplateClass.Classes[NTemplateClass.AppTemplate] };
        }

        public override string Inherits {
            get { throw new System.NotImplementedException(); }
        }

        public override string ClassName {
            get { return UpperFirst(NTemplateClass.NValueClass.ClassName) + "Metadata"; }
        }

        public static string UpperFirst( string str ) {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        public NTemplateClass NTemplateClass;
    }
}
