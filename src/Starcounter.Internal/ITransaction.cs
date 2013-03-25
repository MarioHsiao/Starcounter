using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {
    public interface ITransaction : IDisposable {
        void Commit();
        void Rollback();

    }
}
