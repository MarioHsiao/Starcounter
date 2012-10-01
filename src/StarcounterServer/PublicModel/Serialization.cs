using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Interface of a response serializer.
    /// </summary>
    public interface IResponseSerializer {
        string SerializeReponse(object response);
    }
}
