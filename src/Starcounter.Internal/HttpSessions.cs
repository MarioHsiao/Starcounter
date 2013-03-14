// ***********************************************************************
// <copyright file="HttpStructs.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Starcounter.Advanced;

namespace HttpStructs
{
    /// <summary>
    /// Represents Apps session.
    /// </summary>
    public interface IAppsSession
    {
        /// <summary>
        /// Checks if this session is currently in use.
        /// </summary>
        /// <returns></returns>
        Boolean IsBeingUsed();

        /// <summary>
        /// Destroys the Apps sessions.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Indicates that session is started being used.
        /// </summary>
        void StartUsing();

        /// <summary>
        /// Indicates that session was stopped being used.
        /// </summary>
        void StopUsing();
    }

    /// <summary>
    /// Apps session representation struct.
    /// </summary>
    struct AppsSessionInternal
    {
        // Unique Apps session number.
        public UInt64 apps_session_salt_;

        // Apps session object reference.
        public IAppsSession apps_session_int_;

        // Destroys the instance.
        public void Destroy()
        {
            Console.WriteLine("Destroying session with salt: " + apps_session_salt_);

            // Destroying corresponding Apps session.
            apps_session_int_.Destroy();

            // Resetting the salt also.
            apps_session_salt_ = HttpRequest.INVALID_APPS_SESSION_SALT;
        }
    }

    /// <summary>
    /// Contains all sessions per scheduler.
    /// </summary>
    class SchedulerSessions
    {
        // Maximum number of sessions per scheduler.
        public const Int32 MaxSessionsPerScheduler = 10000;

        // All Apps sessions belonging to the scheduler.
        AppsSessionInternal[] apps_sessions_ = new AppsSessionInternal[MaxSessionsPerScheduler];

        // Number of active sessions on this scheduler.
        UInt64 num_active_sessions_ = 0;

        // List of free sessions.
        UInt64[] free_session_indexes_ = new UInt64[MaxSessionsPerScheduler];

        public SchedulerSessions()
        {
            for (UInt64 i = 0; i < MaxSessionsPerScheduler; i++)
                free_session_indexes_[i] = i;
        }

        // Creates new Apps session.
        public UInt32 CreateNewSession(
            IAppsSession apps_session,
            ref UInt64 session_index,
            ref UInt64 session_salt)
        {
            UInt64 free_session_index = free_session_indexes_[num_active_sessions_];

            apps_sessions_[num_active_sessions_].apps_session_salt_ = session_salt = GlobalSessions.AllGlobalSessions.GenerateSalt();

            apps_sessions_[num_active_sessions_].apps_session_int_ = apps_session;

            session_index = num_active_sessions_;

            num_active_sessions_++;

            return 0;
        }

        // Destroys existing Apps session.
        public UInt32 DestroySession(UInt64 apps_session_index, UInt64 apps_unique_salt)
        {
            // Checking that salt is correct.
            if (apps_sessions_[apps_session_index].apps_session_salt_ == apps_unique_salt)
            {
                // Checking that session is not being used at the moment.
                if (apps_sessions_[apps_session_index].apps_session_int_.IsBeingUsed())
                    throw new Exception("Trying to destroy a session that is already used in some task!");

                // Destroys existing Apps session.
                apps_sessions_[apps_session_index].Destroy();

                num_active_sessions_--;
                free_session_indexes_[num_active_sessions_] = apps_session_index;
            }

            return 0;
        }

        // Gets certain Apps session.
        public IAppsSession GetAppsSessionInterface(UInt64 apps_session_index, UInt64 apps_session_salt)
        {
            AppsSessionInternal s = apps_sessions_[apps_session_index];

            // Checking for the correct session salt.
            if (apps_session_salt == s.apps_session_salt_)
                return apps_sessions_[apps_session_index].apps_session_int_;

            return null;
        }
    }

    /// <summary>
    /// Contains all sessions belonging to Apps.
    /// </summary>
    public class GlobalSessions
    {
        /// <summary>
        /// Maximum number of schedulers.
        /// </summary>
        const Byte MaxSchedulersNumber = 32;

        // All schedulers sessions.
        SchedulerSessions[] scheduler_sessions_ = new SchedulerSessions[MaxSchedulersNumber];

        // Apps session salt.
        Int64 apps_session_salt_ = Int64.MaxValue / 2;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GlobalSessions()
        {
            for (Int32 i = 0; i < MaxSchedulersNumber; i++)
            {
                scheduler_sessions_[i] = new SchedulerSessions();
            }
        }

        /// <summary>
        /// Creates a new sessions.
        /// </summary>
        /// <param name="apps_session"></param>
        /// <param name="scheduler_id"></param>
        /// <param name="session_index"></param>
        /// <param name="session_salt"></param>
        /// <returns></returns>
        public UInt32 CreateNewSession(
            IAppsSession apps_session,
            UInt32 scheduler_id,
            ref UInt64 session_index,
            ref UInt64 session_salt)
        {
            return scheduler_sessions_[scheduler_id].CreateNewSession(
                apps_session,
                ref session_index,
                ref session_salt);
        }

        /// <summary>
        /// Kills existing session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        /// <returns></returns>
        public UInt32 DestroySession(
            UInt64 apps_session_index,
            UInt64 apps_session_salt,
            UInt32 scheduler_id)
        {
            return scheduler_sessions_[scheduler_id].DestroySession(apps_session_index, apps_session_salt);
        }

        /// <summary>
        /// All global sessions.
        /// </summary>
        public static GlobalSessions AllGlobalSessions = new GlobalSessions();

        /// <summary>
        /// Callback to destroy Apps session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        public delegate void DestroyAppsSessionCallback(
            UInt64 apps_session_index,
            UInt64 apps_session_salt,
            UInt32 scheduler_id);

        /// <summary>
        /// Managed callback to destroy Apps session.
        /// </summary>
        public static DestroyAppsSessionCallback g_destroy_apps_session_callback = DestroySessionCallback;

        /// <summary>
        /// Managed callback to destroy Apps session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        public static void DestroySessionCallback(
            UInt64 apps_session_index,
            UInt64 apps_session_salt,
            UInt32 scheduler_id)
        {
            AllGlobalSessions.DestroySession(apps_session_index, apps_session_salt, scheduler_id);
        }

        /// <summary>
        /// Generates session salt.
        /// </summary>
        /// <returns>Generated session.</returns>
        internal UInt64 GenerateSalt()
        {
            return (UInt64)Interlocked.Increment(ref apps_session_salt_);
        }

        /// <summary>
        /// Returns existing Apps session interface.
        /// </summary>
        /// <param name="scheduler_id"></param>
        /// <param name="apps_session_index"></param>
        /// <returns></returns>
        internal IAppsSession GetAppsSessionInterface(
            UInt32 scheduler_id,
            UInt64 apps_session_index,
            UInt64 apps_session_salt)
        {
            return scheduler_sessions_[scheduler_id].GetAppsSessionInterface(apps_session_index, apps_session_salt);
        }
    }

    /// <summary>
    /// Apps session class.
    /// </summary>
    public class AppsSession : IAppsSession
    {
        // Indicates if session is being used by a task.
        Boolean is_used_ = false;

        // Linked list node with this session in it.
        LinkedListNode<AppsSession> node_ = null;

        /// <summary>
        /// Linked list node to itself.
        /// </summary>
        public void SetNode(LinkedListNode<AppsSession> node)
        {
            node_ = node;
        }

        /// <summary>
        /// Linked list node to itself.
        /// </summary>
        public LinkedListNode<AppsSession> GetNode()
        {
            return node_;
        }

        /// <summary>
        /// Checks if this session is currently in use.
        /// </summary>
        /// <returns></returns>
        public Boolean IsBeingUsed()
        {
            return is_used_ == true;
        }

        /// <summary>
        /// Destroys the Apps sessions.
        /// </summary>
        public void Destroy()
        {
            SchedulerAppsSessionsPool.Pool.ReturnBack(this);
            node_ = null;
        }

        /// <summary>
        /// Indicates that session is started being used.
        /// </summary>
        public void StartUsing()
        {
            is_used_ = true;
        }

        /// <summary>
        /// Indicates that session was stopped being used.
        /// </summary>
        public void StopUsing()
        {
            is_used_ = false;
        }
    }

    /// <summary>
    /// Per scheduler pool for allocating Apps sessions.
    /// </summary>
    public class SchedulerAppsSessionsPool
    {
        LinkedList<AppsSession> all_used_apps_ = new LinkedList<AppsSession>();

        /// <summary>
        /// Efficiently allocates a new/existing Apps session.
        /// </summary>
        /// <returns>New Apps session index</returns>
        public AppsSession Allocate()
        {
            // Checking if we have free session indexes.
            if (all_used_apps_.Last != null)
            {
                LinkedListNode<AppsSession> apps_session_node = all_used_apps_.Last;
                all_used_apps_.RemoveLast();
                apps_session_node.Value.SetNode(apps_session_node);
                return apps_session_node.Value;
            }

            // Creating new Apps session instance.
            AppsSession new_session = new AppsSession();

            // Creating new node.
            LinkedListNode<AppsSession> new_node = new LinkedListNode<AppsSession>(new_session);
            new_session.SetNode(new_node);

            return new_session;
        }

        /// <summary>
        /// Returning Apps sessions back to pool.
        /// </summary>
        /// <param name="apps_session"></param>
        public void ReturnBack(AppsSession apps_session)
        {
            all_used_apps_.AddLast(apps_session.GetNode());
        }

        /// <summary>
        /// Scheduler pool instance.
        /// </summary>
        [ThreadStatic]
        static SchedulerAppsSessionsPool pool_;

        /// <summary>
        /// Gets instance of scheduler pool.
        /// </summary>
        public static SchedulerAppsSessionsPool Pool
        {
            get
            {
                if (pool_ == null)
                    pool_ = new SchedulerAppsSessionsPool();

                return pool_;
            }
        }
    }
}