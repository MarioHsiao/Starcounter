using Starcounter.XSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter {

    public interface IChangeLog {
        string GetChanges();
        void Clear();
        void ApplyChanges(string patches);
    }

    internal class ChangeLog : IChangeLog {
        internal Session Session = new Session(SessionOptions.StrictPatchRejection);
        internal Json Root;

        string IChangeLog.GetChanges() {
            var jp = new JsonPatch();
            return jp.CreateJsonPatch(Session, false, false);
        }

        void IChangeLog.Clear() {
            Session.ClearChangeLog();
        }

        void IChangeLog.ApplyChanges(string patches) {
            // Json Root
            // string patches
            // lägga på ändringarna på trädet Root
            // HUR?
            var jp = new JsonPatch();

            byte[] bytes = Encoding.UTF8.GetBytes(patches);

            //            byte[] bytes = new byte[patches.Length * sizeof(char)];
            //            System.Buffer.BlockCopy(patches.ToCharArray(), 0, bytes, 0, bytes.Length);

            jp.EvaluatePatches(Session, bytes);
        }


    }

}
