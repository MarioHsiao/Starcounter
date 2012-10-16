using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class NPrimitiveType : NValueClass
    {
        public override string Inherits
        {
            get { throw new NotImplementedException(); }
        }

        public override string ClassName
        {
            get
            {
                if (NTemplateClass.Template is ActionProperty)
                    return "Action";

                var type = NTemplateClass.Template.InstanceType;
                if (type == typeof(Int32))
                {
                    return "int";
                }
                else if (type == typeof(Boolean))
                {
                    return "bool";
                }
                return type.Name;
            }
        }
    }
}
