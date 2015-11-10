using GenerateMetadataClasses.Model;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;

namespace GenerateMetadataClasses.CodeGenerator {

    public class TypeSpecificationGenerator {
        public const string ClassName = "__starcounterTypeSpecification";
        public const string ThisId = "__sc__this_id__";
        public const string ThisHandle = "__sc__this_handle__";
        public const string ColumnHandlePrefix = "columnHandle_";
        public const string TableHandle = "tableHandle";
        public const string TypeBinding = "typeBinding";

        public readonly Table Table;
        public readonly CodeTypeDeclaration DatabaseClass;

        List<Column> columnHandles = new List<Column>();

        public static string MakeColumnIndexerAccessExpression(string columnName) {
            // A full member access expression
            return string.Format("{0}.{1}", ClassName, MakeColumnIndexer(columnName));
        }

        public static string MakeColumnIndexer(string columnName) {
            return ColumnHandlePrefix + columnName;
        }

        public TypeSpecificationGenerator(Table table, CodeTypeDeclaration databaseClass) {
            Table = table;
            DatabaseClass = databaseClass;
        }

        public void AddColumn(Column c) {
            columnHandles.Add(c);
        }

        public CodeTypeDeclaration Generate() {
            // Generate the class, don't add it.
            var ts = new CodeTypeDeclaration(TypeSpecificationGenerator.ClassName);
            ts.IsClass = true;
            ts.Attributes = MemberAttributes.Static | MemberAttributes.New;
            ts.TypeAttributes = TypeAttributes.NestedAssembly;
            
            var tableHandle = new CodeMemberField();
            tableHandle.Name = TypeSpecificationGenerator.TableHandle;
            tableHandle.Type = new CodeTypeReference(typeof(ushort));
            tableHandle.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;

            var typeBinding = new CodeMemberField();
            typeBinding.Name = TypeSpecificationGenerator.TypeBinding;
            typeBinding.Type = new CodeTypeReference("TypeBinding");
            typeBinding.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;

            ts.Members.AddRange(new[] { tableHandle, typeBinding });

            foreach (var c in columnHandles) {
                var field = new CodeMemberField();
                field.Name = MakeColumnIndexer(c.ColumnName);
                field.Type = new CodeTypeReference(typeof(int));
                field.Attributes = MemberAttributes.Assembly | MemberAttributes.Static;
                ts.Members.Add(field);
            }
            
            return ts;
        }
    }
}
