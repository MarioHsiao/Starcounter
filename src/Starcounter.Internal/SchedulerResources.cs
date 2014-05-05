using System.Threading.Tasks;
using Starcounter.Rest;
using System.IO;
using System;

namespace Starcounter.Internal
{
    class SchedulerResources {

        public const Int32 ResponseTempBufSize = 4096;

        Byte[] response_temp_buf_ = new Byte[ResponseTempBufSize];

        public Byte[] ResponseTempBuf { get { return response_temp_buf_; } }

        public Response AggregationStubResponse = new Response() { Body = "Xaxa!" };

        static SchedulerResources[] all_schedulers_resources_;

        public static void Init(Int32 numSchedulers) {
            all_schedulers_resources_ = new SchedulerResources[numSchedulers];

            for (Int32 i = 0; i < numSchedulers; i++) {
                all_schedulers_resources_[i] = new SchedulerResources();
                all_schedulers_resources_[i].AggregationStubResponse.ConstructFromFields();
            }
        }

        public static SchedulerResources Current {
            get { return all_schedulers_resources_[StarcounterEnvironment.CurrentSchedulerId]; }
        }
    }
}