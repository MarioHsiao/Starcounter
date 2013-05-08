﻿// ***********************************************************************
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
using System.Security.Cryptography;
using Starcounter.Internal;

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

        // Socket number.
        public UInt64 socket_num_;

        // Unique socket id for the gateway.
        public UInt64 socket_unique_id_;

        // Port index.
        public Int32 port_index_;

        // Last active time tick.
        public UInt64 LastActiveTimeTick { get; set; }

        // Apps session object reference.
        public IAppsSession apps_session_int_;

        // Linear index node.
        public LinkedListNode<UInt32> linear_index_node_;

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

            // Removing linear index node.
            linear_index_node_ = null;
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
        public const Int32 MaxSessionsPerScheduler = 100000;

        // All Apps sessions belonging to the scheduler.
        ScSessionClass[] apps_sessions_ = new ScSessionClass[MaxSessionsPerScheduler];

        // List of free sessions.
        LinkedList<UInt32> free_session_indexes_ = new LinkedList<UInt32>();

        // List of used sessions.
        LinkedList<UInt32> used_session_indexes_ = new LinkedList<UInt32>();

        // Random generator for sessions.
        RNGCryptoServiceProvider rand_gen_ = new RNGCryptoServiceProvider();
        Byte[] rand_gen_buf_ = new Byte[8];

        /// <summary>
        /// Generates random salt based on RNGCryptoServiceProvider.
        /// </summary>
        public UInt64 GenerateRandomSalt()
        {
            rand_gen_.GetBytes(rand_gen_buf_);
            return BitConverter.ToUInt64(rand_gen_buf_, 0);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SchedulerSessions()
        {
            for (UInt32 i = 0; i < MaxSessionsPerScheduler; i++)
                free_session_indexes_.AddLast(i);
        }

        /// <summary>
        /// Gets current number of used sessions.
        /// </summary>
        /// <returns></returns>
        public Int32 GetNumberOfActiveSessions()
        {
            return used_session_indexes_.Count;
        }

        // Creates new Apps session.
        public UInt32 CreateNewSession(
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index,
            IAppsSession apps_session_int)
        {
            // Getting free linear session index.
            LinkedListNode<UInt32> linear_index_node = free_session_indexes_.First;
            free_session_indexes_.RemoveFirst();

            // Obtaining linear index.
            linear_index = linear_index_node.Value;

            // Creating new session object if needed.
            if (apps_sessions_[linear_index] == null)
                apps_sessions_[linear_index] = new ScSessionClass();

            // Getting session class reference.
            ScSessionClass s = apps_sessions_[linear_index];

            // Generating random salt.
            random_salt = GenerateRandomSalt();

            // Initializing session structure underneath.
            s.session_struct_.Init(
                scheduler_id,
                linear_index,
                random_salt,
                view_model_index); // TODO

            // Serializing to bytes.
            s.SerializeToBytes();

            // Saving reference to internal session.
            if (apps_session_int != null)
                apps_session_int.InternalSession = apps_sessions_[linear_index];

            // Setting last active time.
            s.LastActiveTimeTick = CurrentTimeTick;

            // Attaching the interface.
            s.apps_session_int_ = apps_session_int;

            // Adding to used sessions.
            used_session_indexes_.AddLast(linear_index_node);

            // Attaching linear index node.
            s.linear_index_node_ = linear_index_node;

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
            ScSessionClass s = apps_sessions_[linear_index];

            // Checking that salt is correct.
            if (s.session_struct_.random_salt_ == random_salt)
            {
                // Checking that session is not being used at the moment.
                if (s.IsBeingUsed())
                    throw new Exception("Trying to destroy a session that is already used in some task!");

                // Removing used session index node.
                LinkedListNode<UInt32> linear_index_node = s.linear_index_node_;
                used_session_indexes_.Remove(linear_index_node);

                // Destroys existing Apps session.
                s.Destroy();

                // Restoring the free index back.
                free_session_indexes_.AddFirst(linear_index_node);
            }

            return 0;
        }

        // Gets certain Apps session.
        public IAppsSession GetAppsSessionInterface(
            UInt32 linear_index,
            UInt64 random_salt)
        {
            // Checking if we are out of range.
            if (linear_index >= used_session_indexes_.Count)
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

        // Sets socket options on session.
        public ScSessionClass GetSessionClass(
            UInt32 linear_index,
            UInt64 random_salt)
        {
            // Checking if we are out of range.
            if (linear_index >= used_session_indexes_.Count)
                return null;

            ScSessionClass s = apps_sessions_[linear_index];
            if (s == null)
                return null;

            // Checking for the correct session salt.
            if (random_salt == s.session_struct_.random_salt_)
            {
                // Returning the session class.
                return s;
            }

            return null;
        }

        // Current per-scheduler time tick.
        public UInt64 CurrentTimeTick = 0;

        // Default session timeout interval in minutes.
        public const Int32 DefaultSessionTimeoutMinutes = 10;

        /// <summary>
        /// Looks up for inactive sessions and kills them. A Timer will schedule this method 
        /// as a job for each scheduler.
        /// </summary>
        public void InactiveSessionsCleanupRoutine()
        {
            try
            {
                //Console.WriteLine("Cleaning up inactive sessions!");
                
                // Incrementing global time.
                CurrentTimeTick++;

                UInt32 num_checked_sessions = 0;
                LinkedListNode<UInt32> used_session_index_node = used_session_indexes_.First;
                while (used_session_index_node != null) {
                    LinkedListNode<UInt32> next_used_session_index_node = used_session_index_node.Next;

                    // Getting session instance.
                    ScSessionClass s = apps_sessions_[used_session_index_node.Value];

                    // Checking if session is created at all.
                    if (s != null) 
                    {
                        // Checking that session is active at all.
                        if (s.session_struct_.IsActive()) 
                        {
                            // Checking that there is an Apps session at all.
                            if (s.apps_session_int_ != null)
                            {
                                // Checking that Apps session is not currently in use.
                                if (!s.apps_session_int_.IsBeingUsed()) 
                                {
                                    // Checking if session is outdated.
                                    if ((CurrentTimeTick - s.LastActiveTimeTick) > 2) 
                                    {
                                        // Destroying old session.
                                        DestroySession(s.session_struct_);
                                    }
                                }
                            }

                            num_checked_sessions++;
                        }
                    } 
                    else 
                    {
                        // NOTE: Apps session was destroyed already so deleting the wrapper.
                        DestroySession(s.session_struct_);
                    }

                    // Getting next used session.
                    used_session_index_node = next_used_session_index_node;

                    // Checking if we have scanned all created sessions.
                    if (num_checked_sessions >= used_session_indexes_.Count)
                        break;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                Environment.Exit((int)Error.SCERRSESSIONMANAGERDIED);
            }
        }
    }

    /// <summary>
    /// Contains all sessions belonging to Apps.
    /// </summary>
    public class GlobalSessions
    {
        // All schedulers sessions.
        SchedulerSessions[] scheduler_sessions_ = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GlobalSessions(Byte num_schedulers)
        {
            scheduler_sessions_ = new SchedulerSessions[num_schedulers];

            for (Int32 i = 0; i < num_schedulers; i++)
            {
                scheduler_sessions_[i] = new SchedulerSessions();
            }
        }

        /// <summary>
        /// Total number of active sessions on all schedulers.
        /// </summary>
        /// <returns></returns>
        public String GetActiveSessionsStats()
        {
            String all_schedulers_stats = "";

            for (Int32 i = 0; i < scheduler_sessions_.Length; i++)
            {
                all_schedulers_stats += "Scheduler " + i + ": " + scheduler_sessions_[i].GetNumberOfActiveSessions() + Environment.NewLine;
            }

            return all_schedulers_stats;
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
        /// Generates a new session on a specific scheduler.
        /// </summary>
        /// <param name="scheduler_id"></param>
        /// <param name="apps_session"></param>
        /// <returns></returns>
        public UInt32 CreateNewSession(
            Byte scheduler_id,
            IAppsSession apps_session)
        {
            // NOTE: Does not matter what values this variables have,
            // since they are not used anyway.
            UInt32 linear_index = 0;
            UInt64 random_salt = 0;
            UInt32 view_model_index = 0;

            return scheduler_sessions_[scheduler_id].CreateNewSession(
                scheduler_id,
                ref linear_index,
                ref random_salt,
                ref view_model_index,
                apps_session);
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
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index,
            IAppsSession apps_session)
        {
            return scheduler_sessions_[scheduler_id].CreateNewSession(
                scheduler_id,
                ref linear_index,
                ref random_salt,
                ref view_model_index,
                apps_session);
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
        /// Gets existing session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        /// <returns></returns>
        public ScSessionClass GetSessionClass(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt)
        {
            return scheduler_sessions_[scheduler_id].GetSessionClass(
                linear_index,
                random_salt);
        }

        /// <summary>
        /// All global sessions.
        /// </summary>
        public static GlobalSessions AllGlobalSessions = null;

        /// <summary>
        /// Creating global sessions.
        /// </summary>
        /// <param name="num_schedulers"></param>
        public static void InitGlobalSessions(Byte num_schedulers)
        {
            AllGlobalSessions = new GlobalSessions(num_schedulers);
        }

        /// <summary>
        /// Callback to destroy Apps session.
        /// </summary>
        /// <param name="scheduler_id"></param>
        /// <param name="linear_index"></param>
        /// <param name="random_salt"></param>
        public delegate void DestroyAppsSessionCallback(
            Byte scheduler_id,
            UInt32 linear_index,
            UInt64 random_salt
            );

        public static DestroyAppsSessionCallback g_destroy_apps_session_callback = DestroySessionCallback;

        /// <summary>
        /// Callback to create new Apps session.
        /// </summary>
        /// <param name="scheduler_id"></param>
        /// <param name="linear_index"></param>
        /// <param name="random_salt"></param>
        /// <param name="view_model_index"></param>
        public delegate void CreateNewAppsSessionCallback(
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index
            );
        
        public static CreateNewAppsSessionCallback g_create_new_apps_session_callback = CreateNewSessionCallback;

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
        /// Managed callback to create new Apps session.
        /// </summary>
        /// <param name="apps_session_index"></param>
        /// <param name="apps_session_salt"></param>
        /// <param name="scheduler_id"></param>
        public static void CreateNewSessionCallback(
            Byte scheduler_id,
            ref UInt32 linear_index,
            ref UInt64 random_salt,
            ref UInt32 view_model_index
            )
        {
            AllGlobalSessions.CreateNewSession(
                scheduler_id,
                ref linear_index,
                ref random_salt,
                ref view_model_index,
                null
                );
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