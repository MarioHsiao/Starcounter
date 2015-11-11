
using GenerateMetadataClasses.Model;
using System.CodeDom;
using System.Text;

namespace GenerateMetadataClasses.CodeGenerator {

    public class PropertyGenerator {

        public CodeTypeMember Generate(Column column) {
            var map = DataTypeMap.Map(column);

            // We use the super-ugly field name exploit hack in CodeDom, because we
            // need the setter to be internal, and also, we can guard for this to work
            // because its build-time, so we never put any end users at risk here.

            var field = new CodeMemberField {
                Attributes = MemberAttributes.Public | MemberAttributes.Final,
                Name = column.ColumnName,
                Type = new CodeTypeReference(map.ReferenceTypeName)
            };

            var b = new StringBuilder();
            b.Append("return ");
            b.Append(map.CastExpression);
            b.Append("DbState.");
            b.Append(map.ReadMethod);
            b.Append("(");
            b.Append(TypeSpecificationGenerator.ThisId);
            b.Append(", ");
            b.Append(TypeSpecificationGenerator.ThisHandle);
            b.Append(", ");
            b.Append(TypeSpecificationGenerator.MakeColumnIndexerAccessExpression(column.ColumnName));
            b.Append(");");
            var readExpression = b.ToString();

            b.Clear();
            b.Append("DbState.");
            b.Append(map.WriteMethod);
            b.Append("(");
            b.Append(TypeSpecificationGenerator.ThisId);
            b.Append(", ");
            b.Append(TypeSpecificationGenerator.ThisHandle);
            b.Append(", ");
            b.Append(TypeSpecificationGenerator.MakeColumnIndexerAccessExpression(column.ColumnName));
            b.Append(", value");
            b.Append(");");
            var writeExpression = b.ToString();
            
            var body = @"{ get { " + readExpression + "} internal set { " + writeExpression + "} }";
            field.Name += body + "//";  // The superhack to prevent CodeDom emission of semicolon to mess up.

            return field;
        }
    }
}