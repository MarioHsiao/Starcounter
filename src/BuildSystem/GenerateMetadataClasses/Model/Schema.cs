using System.Collections.Generic;

namespace GenerateMetadataClasses.Model {

    public class Schema {
        public readonly Dictionary<string, Table> Tables;

        public Schema() {
            Tables = new Dictionary<string, Table>(12);
        }
    }
}
