
using System;
using Starcounter.Internal.Fakeway;
namespace Starcounter.Internal
{
    /*
    /// <summary>
    /// Starcounter unique session id. This is a fake prototype for the real thing.
    /// Probably incorrect, but just a example of what might be going on in the
    /// "real mackoy".
    /// </summary>
    public struct SessionID {
        /// <summary>
        /// Creates a unique session ID.
        /// </summary>
        /// <param name="TcpIpWorkerThreadNo">
        /// The worker thread number.
        /// This prevents us from having to do any locking as  
        /// the sequence number + worker thread number will be unique.
        /// Usually there is only one communication worker thread and 
        /// this value will then always be the same for all session ids.
        /// </param>
        /// </summary>
        public SessionID( byte TcpIpWorkerThreadNo, UInt16 numberOfSchedulers ) {
            RandomNumber = (UInt16)(Random.NextDouble() * 0xFFFFFFFFFFFFFFFF);
            NextFreeSessionSequencyNumbers[TcpIpWorkerThreadNo] += 1; // TODO! Should go back to 1 when reached beyond end
            SequentialNumber = NextFreeSessionSequencyNumbers[TcpIpWorkerThreadNo];
            int scheduler = SchedulerRoundRobin + 1;
            if (scheduler >= numberOfSchedulers) {
                SchedulerRoundRobin = (UInt16)(scheduler = 0);// Does not need to be threadsafe. If two requests go to the same scheduler due to simultanious calls, we are busy enough to get multiple requests per scheduler anyway
            }
            Scheduler = (UInt16)scheduler;
            Channel = 0; // Round robin here as well?
            Connection = null;
        }

        public static SessionID NullSession;

        public static SessionID CreateSession() {
            var sid = new SessionID(1,1);
            return sid;
        }

        public static SessionID RestoreSessionID(UInt32 seqno) {
            var sid = new SessionID();
            sid.SequentialNumber = seqno;
            return sid;
        }

        public bool IsNullSession { get { return SequentialNumber == 0; } }

        static Random Random = new Random();

        /// <summary>
        /// The next scheduler to use for new connections is done by round robin
        /// </summary>
        static UInt16 SchedulerRoundRobin = 0;

        /// <summary>
        /// To be thread safe, the sequence number counter is per worker thread
        /// </summary>
        static UInt32[] NextFreeSessionSequencyNumbers = new UInt32[256];

        /// <summary>
        /// Each worker thread has its own range of sequential numbers in order
        /// to avoid collision. We don't want to rely purely on random numbers
        /// as we could get the same one two times in a row (theoretically).
        /// This avoids probabilistic programming to some degree, unless
        /// a full 24 bit circle is completed with ongoing sessions.
        /// </summary>
        static SessionID() {
            for (int t = 0; t < 256; t++) {
                NextFreeSessionSequencyNumbers[t] = (UInt32)(( 0xFFFFFFFF / 0xFF ) * t);
            }
            NullSession = new SessionID(0, 1) { SequentialNumber = 0 };
        }

        public override string ToString() {
            return SequentialNumber.ToString();
        }



        /// <summary>
        /// TODO! Generated using the new Intel safe random number
        /// </summary>
        public UInt16 RandomNumber; // Should be 64 bit, but Newtonsoft Json Converter chokes on it.

        /// <summary>
        /// Incremented by one. Together with the WorkerThreadNumber, this
        /// be unique for 4 billion sessions
        /// </summary>
        public UInt32 SequentialNumber;

        /// <summary>
        /// Tells the scheduler
        /// </summary>
        public UInt16 Scheduler;

        /// <summary>
        /// Tells the channel
        /// </summary>
        public UInt16 Channel;

        /// <summary>
        /// Used to find the socket for the session
        /// </summary>
        public IConnection Connection;
    }
    */
}
