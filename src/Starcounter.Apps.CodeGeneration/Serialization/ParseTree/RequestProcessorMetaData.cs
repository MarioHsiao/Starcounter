// ***********************************************************************
// <copyright file="RequestProcessorMetaData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using System;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// Class RequestProcessorMetaData
    /// </summary>
    public class RequestProcessorMetaData  {
        /// <summary>
        /// The handler index
        /// </summary>
        public int HandlerIndex;

        /// <summary>
        /// The parameter types
        /// </summary>
        public List<Type> ParameterTypes = new List<Type>();

        /// <summary>
        /// The _ unprepared verb and URI
        /// </summary>
        private string _UnpreparedVerbAndUri;
        /// <summary>
        /// The _ prepared verb and URI
        /// </summary>
        private string _PreparedVerbAndUri = "";

        /// <summary>
        /// Gets or sets the unprepared verb and URI.
        /// </summary>
        /// <value>The unprepared verb and URI.</value>
        public string UnpreparedVerbAndUri {
            set {
                _UnpreparedVerbAndUri = value;
//                _PreparedVerbAndUri = UriTemplatePreprocessor.PreprocessUriTemplate(this);
            }
            get {
                return _UnpreparedVerbAndUri;
            }
        }

        /// <summary>
        /// Gets the prepared verb and URI.
        /// </summary>
        /// <value>The prepared verb and URI.</value>
        public string PreparedVerbAndUri {
            get {
                return _PreparedVerbAndUri;
            }
        }

        /// <summary>
        /// Returns true if any of the parameters in the ParameterTypes list
        /// is a subclass of RequestHandler and should be deserialized before
        /// invoking the handler.
        /// </summary>
        public bool NeedsContentDeserialization {
            get {
                bool ret = false;
                foreach (Type t in ParameterTypes) {
//                    if (t.IsSubclassOf(typeof(RequestHandler))) {
//                        ret = true;
//                        break;
//                    }
                }
                return ret;
            }
        }

        /// <summary>
        /// The ast class
        /// </summary>
       // internal AstRequestProcessorClass AstClass;
        /// <summary>
        /// The code
        /// </summary>
        public object Code;

        /// <summary>
        /// Hander identifier.
        /// </summary>
        public Int32 HanderId;
    }

}