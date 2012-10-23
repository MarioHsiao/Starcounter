// ***********************************************************************
// <copyright file="Serialization.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace Starcounter.Server.PublicModel {

    /// <summary>
    /// Interface of a response serializer.
    /// </summary>
    internal interface IResponseSerializer {
        string SerializeResponse(object response);
    }

    internal sealed class NewtonSoftJsonSerializer : IResponseSerializer {
        readonly ServerEngine engine;

        internal NewtonSoftJsonSerializer(ServerEngine server) {
            this.engine = server;
        }

        public string SerializeResponse(object response) {
            return JsonConvert.SerializeObject(response);
        }
    }
}