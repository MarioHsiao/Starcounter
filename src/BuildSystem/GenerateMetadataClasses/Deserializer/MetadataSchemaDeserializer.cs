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
            string globalBaseTableName = null;
            foreach (var t in tables) {
                if (String.IsNullOrEmpty(t.BaseTableName)) {
                    Debug.Assert(String.IsNullOrEmpty(globalBaseTableName), 
                        "Metadata are expected to inherit from only one global base table.");
                    globalBaseTableName = t.TableName;
                }
                else {
                    Debug.Assert(!String.IsNullOrEmpty(globalBaseTableName),
                        "It is expected that the global base table is defined the first, before other meta-tables are defined.");
                    if (t.BaseTableName.Equals(globalBaseTableName))
                        t.BaseTableName = null;
                    t.Schema = s;
                    s.Tables.Add(t.TableName, t);
                }
            }


            return s;
        }
    }
}
