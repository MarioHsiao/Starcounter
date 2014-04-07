using Starcounter.Metadata;
using System;
using System.Text;

namespace Starcounter {
    internal class Reload {
        internal static int Unload(string fileName) {
            int totalNrObj = 0;
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                int tblNrObj = 0;
                StringBuilder inStmt = new StringBuilder();
                StringBuilder selectObjs = new StringBuilder();
                inStmt.Append("INSERT INTO ");
                inStmt.Append(tbl.FullNameReversed);
                inStmt.Append("(__id");
                selectObjs.Append("SELECT o as __id");
                foreach (TableColumn col in Db.SQL<TableColumn>("select c from tablecolumn c where basetable = ?", tbl)) {
                    inStmt.Append(",");
                    inStmt.Append(col.Name);
                }
                inStmt.Append(")");
                selectObjs.Append(" FROM ");
                selectObjs.Append(tbl.FullNameReversed);
                totalNrObj += tblNrObj;
            }
            return totalNrObj;
        }
        internal static int Load(string filename) {
            int totalNrObj = 0;
            return totalNrObj;
        }
    }
}
