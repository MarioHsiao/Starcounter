
using Starcounter.Query;

namespace Starcounter.Internal
{
    
    public static class Fix // TODO:
    {

        public static void ResetTheQueryModule()
        {
            QueryModule.Initiate(true, new Starcounter.Configuration.DatabaseEngineInstanceConfiguration());
        }
    }
}
