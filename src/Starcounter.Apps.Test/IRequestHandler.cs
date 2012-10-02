
using System;
using HttpStructs;

namespace Starcounter {

    public interface IRequestHandler {

        void ProcessRequestBatch(HttpRequest requestBatch);
    }
}