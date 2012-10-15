using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class CodeBehindMetadata
    {
        public static readonly CodeBehindMetadata Empty
            = new CodeBehindMetadata("", new List<JsonMapInfo>(), new List<InputBindingInfo>());

        public readonly String RootNamespace;
        public readonly List<JsonMapInfo> JsonPropertyMapList;
        public readonly List<InputBindingInfo> InputBindingList;

        internal CodeBehindMetadata(String ns, 
                                    List<JsonMapInfo> mapList, 
                                    List<InputBindingInfo> inputList)
        {
            RootNamespace = ns;
            JsonPropertyMapList = mapList;
            InputBindingList = inputList;
        }
    }
}
