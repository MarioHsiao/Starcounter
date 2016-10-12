
using Starcounter.Bootstrap;
using Starcounter.Bootstrap.RuntimeHosts;
using Starcounter.Ioc;
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
            var control = RuntimeHost.CreateAndInitialize(LogSources.Hosting);

            // The prerequisite to use this: backend services running with a
            // running scdata, but with no host.
            //   staradmin start server
            //   staradmin start db
            //   staradmin stop host
            //
            // About ensuring that no other host is running: will that be
            // detected by running ANY two hosts really? I think so. Try that.

            // GETTING this: http://localhost:8181/api/engines/default/db will
            // reveil if a database backend is running. If its not, an EMPTY
            // 404 will indicate that. If the database don't exist, a 404 with
            // a corresponding error detail will be returnd. If the status is
            // success, we are good to go.
            // TODO:

            // Any host must be allowed to fine-tune bootstraping, to control
            // certain aspects of it. For example, the self-host want to make
            // sure the application entrypoint does not get invoked, and also
            // that management API's are not exposed (I think). Possibly more.
            // Console redirects? Probably not.
            // TODO:

            control.RunUntilExit(() => { return configuration; }, entrypoint);
        }
    }
}
