using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class CodeBehindMetadata
    {
        public static readonly CodeBehindMetadata Empty
            = new CodeBehindMetadata("", new List<JsonMapInfo>(), new List<HandleInputInfo>());

        public readonly String RootNamespace;
        public readonly List<JsonMapInfo> JsonPropertyMapList;
        public readonly List<HandleInputInfo> HandleInputList;

        internal CodeBehindMetadata(String ns, 
                                    List<JsonMapInfo> mapList, 
                                    List<HandleInputInfo> inputList)
        {
            RootNamespace = ns;
            JsonPropertyMapList = mapList;
            HandleInputList = inputList;
        }
    }
}
