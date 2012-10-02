using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RapidMinds.BuildSystem.Common
{
    public interface IBuilder
    {

        string SolutionName { get; }

        void Prepare(Configuration configuration);
        VersionInfo Build(Version version, Configuration configuration);

    }
}
