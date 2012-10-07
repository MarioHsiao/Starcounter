
using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {
    public abstract class NValueClass : NClass {

        public NTemplateClass NTemplateClass { get; set; }

        public static Dictionary<Template, NValueClass> Classes = new Dictionary<Template, NValueClass>();

        public static NValueClass Find(Template template) {
            template = NTemplateClass.GetPrototype(template);
            return NValueClass.Classes[template];
        }

        static NValueClass() {
            Classes[NTemplateClass.StringProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.StringProperty] };
            Classes[NTemplateClass.IntProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.IntProperty] };
            Classes[NTemplateClass.DecimalProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DecimalProperty] };
            Classes[NTemplateClass.DoubleProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.DoubleProperty] };
            Classes[NTemplateClass.BoolProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.BoolProperty] };
            Classes[NTemplateClass.ActionProperty] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.ActionProperty] };
            Classes[NTemplateClass.AppTemplate] = new NPrimitiveType { NTemplateClass = NTemplateClass.Classes[NTemplateClass.AppTemplate] };
        }


    }
}
