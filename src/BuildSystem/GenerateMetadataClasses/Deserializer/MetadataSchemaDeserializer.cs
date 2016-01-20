using GenerateMetadataClasses.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GenerateMetadataClasses.Deserializer {

    public class MetadataSchemaDeserializer {
        public readonly string FileName;

        public MetadataSchemaDeserializer(string file) {
            FileName = file;
        }

        public Schema Deserialize() {
            if (!File.Exists(FileName)) {
                throw new Exception(string.Format("Schema file {0} doesnt exist.", FileName));
            }
            var json = File.ReadAllText(FileName);
            var tables = JsonDeserializer.JsonDeserialize<List<Table>>(json);
            
            var s = new Schema();
            foreach (var t in tables) {
                if (String.IsNullOrEmpty(t.BaseTableName)) {
                    Debug.Assert(t.TableName.Equals("MotherOfAllLayouts"));
                }
                else {
                    if (t.BaseTableName.Equals("MotherOfAllLayouts"))
                        t.BaseTableName = null;
                    t.Schema = s;
                    s.Tables.Add(t.TableName, t);
                }
            }


            return s;
        }
    }
}
