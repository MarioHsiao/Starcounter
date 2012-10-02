

namespace Starcounter.Internal {
    public interface IWorker {
        void DoSomeWork( int maxtime );
        bool WantsToStop();
        void Start( bool inNewThread );
    }
}
