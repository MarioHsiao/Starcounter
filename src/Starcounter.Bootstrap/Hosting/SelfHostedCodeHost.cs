
using Starcounter.Bootstrap;
using Starcounter.Ioc;
using StarcounterInternal.Bootstrap;
using System;

namespace Starcounter.Hosting
{
    internal class SelfHostedCodeHost : ICodeHost
    {
        readonly IHostConfiguration configuration;

        public SelfHostedCodeHost(IHostConfiguration config)
        {
            configuration = config;
        }

        public IServices Services {
            get {
                throw new NotImplementedException();
            }
        }

        public void Run(Action entrypoint)
        {
            var control = Control.CreateAndInitialize(LogSources.Hosting);
            control.RunUntilExit(() => { return configuration; }, entrypoint);
        }
    }
}
