

using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration {

    public abstract class NTemplateClass : NClass {

        public static StringProperty StringProperty = new StringProperty();
        public static IntProperty IntProperty = new IntProperty();
        public static DecimalProperty DecimalProperty = new DecimalProperty();
        public static AppTemplate AppTemplate = new AppTemplate();
        public static DoubleProperty DoubleProperty = new DoubleProperty();
        public static BoolProperty BoolProperty = new BoolProperty();
        public static ActionProperty ActionProperty = new ActionProperty();

        public static Dictionary<Template, NTemplateClass> Classes = new Dictionary<Template, NTemplateClass>();

        public Template Template;

        private NValueClass _NValueClass;

        public NValueClass NValueClass {
            get {
                if (_NValueClass != null)
                    return _NValueClass;
                return NValueClass.Classes[this.Template];
            }
            set { _NValueClass = value; }
        }


        public NMetadataClass NMetadataClass { get; set; }

        static NTemplateClass() {
            Classes[StringProperty] = new NPropertyClass {Template = StringProperty};
            Classes[IntProperty] = new NPropertyClass {Template = IntProperty};
            Classes[DecimalProperty] = new NPropertyClass {Template = DecimalProperty};
            Classes[DoubleProperty] = new NPropertyClass {Template = DoubleProperty};
            Classes[BoolProperty] = new NPropertyClass {Template = BoolProperty};
            Classes[ActionProperty] = new NPropertyClass {Template = ActionProperty};
            Classes[AppTemplate] = new NAppTemplateClass {Template = AppTemplate};
        }


        public static NTemplateClass Find(Template template) {
            template = GetPrototype(template);
            return NTemplateClass.Classes[template];
        }

        internal static Template GetPrototype(Template template) {
            if (template is StringProperty) {
                return StringProperty;
            }
            else if (template is IntProperty) {
                return IntProperty;
            }
            else if (template is DoubleProperty) {
                return DoubleProperty;
            }
            else if (template is DecimalProperty) {
                return DecimalProperty;
            }
            else if (template is BoolProperty) {
                return BoolProperty;
            }
            else if (template is ActionProperty) {
                return ActionProperty;
            }
            return template;
        }

        public override string ClassName {
            get {
                return Template.GetType().Name;
            }
        }
    }
}
