using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HttpStructs;

namespace Starcounter.Internal.Application {
    public class SessionDictionary {

        private static Session Hardcoded;
//        private Dictionary<SessionID, Session> Sessions = new Dictionary<SessionID,Session>();

        //public App GetRootApp(SessionID session)
        //{
        //    return RootApps[session];
        //}

        internal Session CreateSession() {
//            var sid = SessionID.CreateSession();
            var session = new Session(1);
            Hardcoded = session;
//            Sessions[sid] = session;
            return session;
        }

        internal Session GetSession(Int32 id)
        {
            //Session session;
            //Sessions.TryGetValue(sid, out session);
            //return session;
            return Hardcoded;
        }

        public Session GetSession(string sessionString)
        {
            // TODO:
            // How do we convert string to SessionID???
            //SessionID sid = SessionID.NullSession;
            //return GetSession(sid);
            return Hardcoded;
        }
    }

    public class Session
    {
        [ThreadStatic]
        private static Session _current;

        internal ChangeLog _changeLog;
        private Int32 _sessionID;
        private App _rootApp;
        private HttpRequest _request;
        
        public static Session Current { get { return _current; } }

        internal Session()
            : this(1)
        {
        }

        internal Session(Int32 sid)
        {
            _changeLog = new ChangeLog();
            _sessionID = sid;
        }

        internal void AttachRootApp(App rootApp)
        {
            _rootApp = rootApp;
        }

        public App RootApp
        {
            get { return _rootApp; }
        }

        public HttpRequest HttpRequest
        {
            get { return _request; }
        }

        internal void Execute(HttpRequest request, Action action)
        {
            try
            {
                _request = request;
                _current = this;
                ChangeLog.BeginRequest(_changeLog);
                action();
            }
            finally
            {
                ChangeLog.EndRequest();
                _current = null;
                _request = null;
            }
        }
    }
}
