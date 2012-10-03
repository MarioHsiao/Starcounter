using System.Collections.Generic;
using System;

namespace Starcounter.Internal.Uri {
    public class RequestProcessorMetaData  {
        public int HandlerIndex;
        public List<Type> ParameterTypes = new List<Type>();

        private string _UnpreparedVerbAndUri;
        private string _PreparedVerbAndUri;

        public string UnpreparedVerbAndUri {
            set {
                _PreparedVerbAndUri = value + " ";
                _UnpreparedVerbAndUri = value;
            }
            get {
                return _UnpreparedVerbAndUri;
            }
        }

        public string PreparedVerbAndUri {
            get {
                return _PreparedVerbAndUri;
            }
        }


        internal AstRequestProcessorClass AstClass;
        public object Code;
    }

}