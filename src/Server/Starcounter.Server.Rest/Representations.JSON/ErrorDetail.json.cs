﻿
using Starcounter;

namespace Starcounter.Server.Rest.Representations.JSON {
    /// <summary>
    /// Outlines an error entity, passed back as the entity
    /// for responses with HTTP statuses in the 4xx- and 5xx
    /// range.
    /// </summary>
    partial class ErrorDetail : Json<object> {
    }
}