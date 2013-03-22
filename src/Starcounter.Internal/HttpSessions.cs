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

        /// <summary>
        /// Getting internal session.
        /// </summary>
        ScSessionClass InternalSession { get; set; }
    }

    // Session structure 128 bits:
    //
    // | Scheduler Id |  Linear Index | Session Salt | View Model Number |
    // |    1 byte    |    3 bytes    |    8 bytes   |      4 bytes      |
    //

    /// <summary>
    /// Apps session representation struct.
    /// </summary>
    public class ScSessionClass
    {
        // Internal session structure.
        public ScSessionStruct session_struct_;

        // Last active time tick.
        public UInt64 LastActiveTimeTick { get; set; }

        // Apps session object reference.
        public IAppsSession apps_session_int_;

        // Is being used.
        public Boolean IsBeingUsed()
        {
            if (apps_session_int_ != null)
                return apps_session_int_.IsBeingUsed();

            return false;
        }

        // Destroys the instance.
        public void Destroy()
        {
            //Console.WriteLine("Destroying session with salt: " + session_struct_.random_salt_);

            // Destroying corresponding Apps session.
            if (apps_session_int_ != null)
                apps_session_int_.Destroy();

            // Destroying session string.
            session_string_ = null;

            // Resetting some fields.
            session_struct_.Destroy();
        }

        public const Int32 SESSION_STRING_NUM_BYTES = 32;

        // Session stored in ASCII bytes.
        Byte[] session_bytes_ = new Byte[SESSION_STRING_NUM_BYTES];

        // Session string representation.
        String session_string_ = null;

        // Converts internal bytes to session.
        public String ToAsciiString()
        {
            if (session_string_ == null)
                session_string_ = Encoding.ASCII.GetString(session_bytes_);

            return session_string_;
        }

        // Serializing session structure to bytes.
        public void SerializeToBytes()
        {
            uint64_to_hex_string(session_struct_.scheduler_id_, session_bytes_, 0, 2);
            uint64_to_hex_string(session_struct_.linear_index_, session_bytes_, 2, 8);
            uint64_to_hex_string(session_struct_.random_salt_, session_bytes_, 8, 16);
            uint64_to_hex_string(session_struct_.view_model_index_, session_bytes_, 24, 8);
        }

        static Byte[] hex_table = { (Byte)'0', (Byte)'1', (Byte)'2', (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'A', (Byte)'B', (Byte)'C', (Byte)'D', (Byte)'E', (Byte)'F' };
        
        // Converts uint64 number to hexadecimal string.
        Int32 uint64_to_hex_string(UInt64 number, Byte[] str_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while(number > 0)
            {
                str_out[offset + n] = hex_table[number & 0xF];
                n++;
                number >>= 4;
            }

            // Filling with zero values if necessary.
            while (n < num_4bits)
            {
                str_out[offset + n] = (Byte)'0';
                n++;
            }

            // Returning length.
            return n;
        }

        // Invalid value of converted number from hexadecimal string.
        const UInt64 INVALID_CONVERTED_NUMBER = 0xFFFFFFFFFFFFFFFF;

        // Converts hexadecimal string to uint64.
        UInt64 hex_string_to_uint64(Byte[] str_in, Int32 offset, Int32 num_4bits)
        {
            UInt64 result = 0;
            Int32 i = offset, s = 0;

            for (Int32 n = 0; n < num_4bits; n++)
            {
                switch(str_in[i])
                {
                    case (Byte)'0': result |= ((UInt64)0 << s); break;
                    case (Byte)'1': result |= ((UInt64)1 << s); break;
                    case (Byte)'2': result |= ((UInt64)2 << s); break;
                    case (Byte)'3': result |= ((UInt64)3 << s); break;
                    case (Byte)'4': result |= ((UInt64)4 << s); break;
                    case (Byte)'5': result |= ((UInt64)5 << s); break;
                    case (Byte)'6': result |= ((UInt64)6 << s); break;
                    case (Byte)'7': result |= ((UInt64)7 << s); break;
                    case (Byte)'8': result |= ((UInt64)8 << s); break;
                    case (Byte)'9': result |= ((UInt64)9 << s); break;
                    case (Byte)'A': result |= ((UInt64)0xA << s); break;
                    case (Byte)'B': result |= ((UInt64)0xB << s); break;
                    case (Byte)'C': result |= ((UInt64)0xC << s); break;
                    case (Byte)'D': result |= ((UInt64)0xD << s); break;
                    case (Byte)'E': result |= ((UInt64)0xE << s); break;
                    case (Byte)'F': result |= ((UInt64)0xF << s); break;

                    // INVALID_CONVERTED_NUMBER should never be returned in normal case.
                    default: return INVALID_CONVERTED_NUMBER;
                }

                i++;
                s += 4;
            }

            return result;
        }
    }

    /// <summary>
    /// Contains all sessions per scheduler.
    /// </summary>
    public class SchedulerSessions
    {
        // Maximum number of sessions per scheduler.
        public const Int32 MaxSessionsPerScheduler = 10000;

        // All Apps sessions belonging to the scheduler.
        ScSessionClass[] apps_sessions_ = new ScSessionClass[MaxSessionsPerScheduler];

        // Number of active sessions on this scheduler.
        UInt32 num_active_sessions_ = 0;

        // List of free sessions.
        UInt32[] free_session_indexes_ = new UInt32[MaxSessionsPerScheduler];

        public SchedulerSessions()
        {
            for (UInt32 i = 0; i < MaxSessionsPerScheduler; i++)
                free_session_indexes_[i] = i;
        }

        // Creates new Apps session.
        public UInt32 CreateNewSession(
            IAppsSession apps_session_int,
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index)
        {
            // Getting free linear session index.
            linear_index = free_session_indexes_[num_active_sessions_];

            // Creating new session object if needed.
            if (apps_sessions_[linear_index] == null)
                apps_sessions_[linear_index] = new ScSessionClass();

            // Generating random salt.
            random_salt = GlobalSessions.AllGlobalSessions.GenerateSalt();

            // Initializing session structure underneath.
            apps_sessions_[linear_index].session_struct_.Init(
                scheduler_id,
                linear_index,
                random_salt,
                view_model_index); // TODO

            // Serializing to bytes.
            apps_sessions_[linear_index].SerializeToBytes();

            // Saving reference to internal session.
            apps_session_int.InternalSession = apps_sessions_[linear_index];

            // Setting last active time.
            apps_sessions_[linear_index].LastActiveTimeTick = CurrentTimeTick;

            // Attaching the interface.
            apps_sessions_[linear_index].apps_session_int_ = apps_session_int;

            // New session has been created.
            num_active_sessions_++;

            return 0;
        }

        // Destroys existing Apps session.
        public UInt32 DestroySession(ScSessionStruct s)
        {
            return DestroySession(s.linear_index_, s.random_salt_);
        }

        // Destroys existing Apps session.
        public UInt32 DestroySession(UInt32 linear_index, UInt64 random_salt)
        {
            // Checking that salt is correct.
            if (apps_sessions_[linear_index].session_struct_.random_salt_ == random_salt)
            {
                // Checking that session is not being used at the moment.
                if (apps_sessions_[linear_index].IsBeingUsed())
                    throw new Exception("Trying to destroy a session that is already used in some task!");

                // Destroys existing Apps session.
                apps_sessions_[linear_index].Destroy();

                // Restoring the free index back.
                num_active_sessions_--;
                free_session_indexes_[num_active_sessions_] = linear_index;
            }

            return 0;
        }

        // Gets certain Apps session.
        public IAppsSession GetAppsSessionInterface(
            UInt32 linear_index,
            UInt64 random_salt)
        {
            // Checking if we are out of range.
            if (linear_index >= num_active_sessions_)
                return null;

            ScSessionClass s = apps_sessions_[linear_index];
            if (s == null)
                return null;

            // Checking for the correct session salt.
            if (random_salt == s.session_struct_.random_salt_)
            {
                // Setting last active time.
                s.LastActiveTimeTick = CurrentTimeTick;

                // Returning the interface.
                return s.apps_session_int_;
            }

            return null;
        }

        // Current per-scheduler time tick.
        public UInt64 CurrentTimeTick = 0;

        // Default session timeout interval in minutes.
        public const Int32 DefaultSessionTimeoutMinutes = 10;

        /// <summary>
        /// Looks up for inactive sessions and kills them.
        /// </summary>
        public void InactiveSessionsCleanupRoutine()
        {
            while (true)
            {
                Console.WriteLine("Cleaning up inactive sessions!");

                // Incrementing global time.
                CurrentTimeTick++;

                // Sleeping given minutes.
                Thread.Sleep(1000 * 60 * DefaultSessionTimeoutMinutes);

                UInt32 num_checked_sessions = 0;
                for (UInt32 i = 0; i < MaxSessionsPerScheduler; i++)
                {
                    // Checking if session is created at all.
                    if (apps_sessions_[i] != null)
                    {
                        // Checking that session is active at all.
                        if (apps_sessions_[i].session_struct_.IsActive())
                        {
                            // Checking that session is not currently in use.
                            if (!apps_sessions_[i].apps_session_int_.IsBeingUsed())
                            {
                                // Checking if session is outdated.
                                if ((CurrentTimeTick - apps_sessions_[i].LastActiveTimeTick) > 2)
                                {
                                    // Destroying old session.
                                    DestroySession(apps_sessions_[i].session_struct_);
                                }
                            }

                            num_checked_sessions++;
                        }
                    }

                    // Checking if we have scanned all created sessions.
                    if (num_checked_sessions >= num_active_sessions_)
                        break;
                }
            }
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
        /// Getting scheduler sessions.
        /// </summary>
        /// <param name="sched_index"></param>
        /// <returns></returns>
        public SchedulerSessions GetSchedulerSessions(Byte sched_index)
        {
            return scheduler_sessions_[sched_index];
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
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index)
        {
            return scheduler_sessions_[scheduler_id].CreateNewSession(
                apps_session,
                scheduler_id,
                ref linear_index,
                ref random_salt,
                ref view_model_index);
        }

        /// <summary>
        /// Kills existing session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        /// <returns></returns>
        public UInt32 DestroySession(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt)
        {
            return scheduler_sessions_[scheduler_id].DestroySession(
                linear_index,
                random_salt);
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
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt
            );

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
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt
            )
        {
            AllGlobalSessions.DestroySession(
                scheduler_id,
                linear_index,
                random_salt
                );
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
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt)
        {
            return scheduler_sessions_[scheduler_id].GetAppsSessionInterface(
                linear_index,
                random_salt);
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
        /// Getting internal session.
        /// </summary>
        public ScSessionClass InternalSession { get; set; }

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