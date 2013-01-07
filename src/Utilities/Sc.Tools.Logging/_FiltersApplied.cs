
using System;

namespace Sc.Tools.Logging
{

[Flags]
internal enum FiltersApplied
{
    None = 0,
    Type = 1,
    FromDateTime = 2,
    ToDateTime = 4,
    ActivityID = 8,
    MachineName = 16,
    ServerName = 32,
    Source = 64,
    Category = 128
}
}
