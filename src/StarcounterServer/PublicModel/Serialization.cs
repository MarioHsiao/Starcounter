using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Interface of a response serializer.
    /// </summary>
    public interface IResponseSerializer {
        string SerializeReponse(object response);
    }

    internal sealed class NewtonSoftJsonSerializer : IResponseSerializer {
        MethodInfo jsonSerializeObject;

        internal NewtonSoftJsonSerializer(ServerNode server) {
            var jsonPath = Path.Combine(server.InstallationDirectory, "Newtonsoft.Json.dll");
            if (File.Exists(jsonPath)) {
                var assembly = Assembly.LoadFrom(jsonPath);
                var converter = assembly.GetType("Newtonsoft.Json.JsonConvert");
                if (converter != null) {
                    jsonSerializeObject = converter.GetMethod("SerializeObject", new Type[] { typeof(object) });
                }
            }
        }

        public string SerializeReponse(object response) {
            return (string) jsonSerializeObject.Invoke(null, new object[] { response });
        }
    }
}
