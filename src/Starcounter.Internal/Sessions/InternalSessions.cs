﻿// ***********************************************************************
// <copyright file="InternalSessions.cs" company="Starcounter AB">
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
using Starcounter;

namespace Starcounter.Internal
{
    /// <summary>
    /// Represents Apps session.
    /// </summary>
    public interface IAppsSession {
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

        /// <summary>
        /// Currently active WebSocket.
        /// </summary>
        WebSocket ActiveWebSocket { get; set; }
    }

    /// <summary>
    /// Represents a database session.
    /// </summary>
    internal interface IDbSession {
        /// <summary>
        /// Runs a task asynchronously on a given scheduler.
        /// </summary>
        void RunAsync(Action action, Byte schedId = Byte.MaxValue);

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        void RunSync(Action action, Byte schedId = Byte.MaxValue);
    }

    public class UriHelper
    {
        /// <summary>
        /// Gets method enumeration from given string.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MixedCodeConstants.HTTP_METHODS GetMethodFromString(String method)
        {
            foreach (MixedCodeConstants.HTTP_METHODS m in Enum.GetValues(typeof(MixedCodeConstants.HTTP_METHODS)))
            {
                if (method.StartsWith(m.ToString()))
                    return m;
            }

            return MixedCodeConstants.HTTP_METHODS.OTHER;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SessionIdLowerUpper {
        public UInt64 IdLower;
        public UInt64 IdUpper;
    }

    /// <summary>
    /// Struct ScSessionStruct
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ScSessionStruct
    {
        // Unique random salt.
        public UInt64 randomSalt_;

        // Session linear index.
        public UInt32 linearIndex_;

        // Scheduler id.
        public Byte schedulerId_;

        // Gateway worker id.
        public Byte gwWorkerId_;

        /// <summary>
        /// Creates session struct from lower and upper parts.
        /// </summary>
        public static ScSessionStruct FromLowerUpper(
            UInt64 idLower, UInt64 idUpper) {

            unsafe {

                SessionIdLowerUpper lowerUpper = new SessionIdLowerUpper() {
                    IdLower = idLower,
                    IdUpper = idUpper
                };

                ScSessionStruct s = *(ScSessionStruct*)(&lowerUpper);

                return s;
            }
        }

        /// <summary>
        /// Creates socket struct from lower and upper parts.
        /// </summary>
        public static void ToLowerUpper(
            ScSessionStruct s,
            out UInt64 idLower,
            out UInt64 idUpper) {

            unsafe {

                SessionIdLowerUpper id = *(SessionIdLowerUpper*)(&s);

                idLower = id.IdLower;
                idUpper = id.IdUpper;
            }
        }
        
        /// <summary>
        /// Constructor.
        /// </summary>
        public ScSessionStruct(Boolean initDefault) {
            randomSalt_ = 0;
            linearIndex_ = UInt32.MaxValue;
            schedulerId_ = StarcounterEnvironment.CurrentSchedulerId;
            gwWorkerId_ = Byte.MaxValue;
        }

        /// <summary>
        /// Initializes session struct.
        /// </summary>
        public void Init(
            Byte schedulerId,
            UInt32 linearIndex,
            UInt64 randomSalt,
            Byte gwWorkerId)
        {
            schedulerId_ = schedulerId;
            linearIndex_ = linearIndex;
            randomSalt_ = randomSalt;
            gwWorkerId_ = gwWorkerId;
        }

        // Checks if this session is active.
        public Boolean IsAlive()
        {
            return linearIndex_ != Request.INVALID_APPS_SESSION_INDEX;
        }

        // Destroys existing session.
        public void Destroy()
        {
            linearIndex_ = Request.INVALID_APPS_SESSION_INDEX;
            randomSalt_ = Request.INVALID_APPS_SESSION_SALT;
        }

        // Print current session.
        public void PrintSession()
        {
            Console.WriteLine(String.Format("Session: scheduler={0}, index={1}, salt={2}, gwworkerid={3}.",
                schedulerId_,
                linearIndex_,
                randomSalt_,
                gwWorkerId_));
        }

        /// <summary>
        /// Initializes session struct from bytes.
        /// </summary>
        /// <param name="str">Input string.</param>
        internal void ParseFromString(String str)
        {
            Byte[] strBytes = Encoding.ASCII.GetBytes(str);

            randomSalt_ = (UInt64) hex_string_to_uint64(strBytes, 0, 16);
            linearIndex_ = (UInt32) hex_string_to_uint64(strBytes, 16, 6);
            schedulerId_ = (Byte) hex_string_to_uint64(strBytes, 22, 2);
        }

        // Serializing session structure to bytes.
        public void SerializeToBytes(Byte[] session_bytes)
        {
            uint64_to_hex_string(randomSalt_, session_bytes, 0, 16);
            uint64_to_hex_string(linearIndex_, session_bytes, 16, 6);
            uint64_to_hex_string(schedulerId_, session_bytes, 22, 2);
        }

        static Byte[] hex_table = { (Byte)'0', (Byte)'1', (Byte)'2', (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'A', (Byte)'B', (Byte)'C', (Byte)'D', (Byte)'E', (Byte)'F' };

        // Converts uint64 number to hexadecimal string.
        public static Int32 uint64_to_hex_string(UInt64 number, Byte[] str_out, Int32 offset, Int32 num_4bits)
        {
            Int32 n = 0;
            while (number > 0)
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
        public static UInt64 hex_string_to_uint64(Byte[] str_in, Int32 offset, Int32 num_4bits)
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
    /// Apps session representation struct.
    /// </summary>
    public class ScSessionClass
    {
        /// <summary>
        /// Get current session instance.
        /// </summary>
        /// <returns></returns>
        public static Func<ScSessionClass> GetCurrent = () => {
            return null;
        };

        /// <summary>
        /// Internal session structure.
        /// </summary>
        public ScSessionStruct session_struct_;

        /// <summary>
        /// Apps session object reference.
        /// </summary>
        public IAppsSession apps_session_int_;

        /// <summary>
        /// Linear index node.
        /// </summary>
        public LinkedListNode<UInt32> linear_index_node_;

        /// <summary>
        /// Using session cookie.
        /// </summary>
        public Boolean use_session_cookie_;

        /// <summary>
        /// Specific saved user object ID.
        /// </summary>
        public UInt64 CargoId { get; set; }

        /// <summary>
        /// Time when session was created.
        /// </summary>
        public DateTime Created { get; internal set; }

        /// <summary>
        /// Last time the session was active.
        /// </summary>
        public DateTime LastActive { get; internal set; }

        /// <summary>
        /// Last active time tick.
        /// </summary>
        public UInt64 LastActiveTimeTick { get; set; }

        /// <summary>
        /// Timeout minutes.
        /// </summary>
        public UInt64 TimeoutMinutes { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ScSessionClass()
        {
            TimeoutMinutes = StarcounterEnvironment.Default.SessionTimeoutMinutes;
        }

        /// <summary>
        /// Database session interface.
        /// </summary>
        static IDbSession _dbSession;

        /// <summary>
        /// Database session.
        /// </summary>
        internal static IDbSession DbSession
        {
            get { return _dbSession; }
        }

        /// <summary>
        /// Setting actual database session implementation.
        /// </summary>
        internal static unsafe void SetDbSessionImplementation(IDbSession dbSessionImpl)
        {
            _dbSession = dbSessionImpl;
        }

        /// <summary>
        /// Runs a task asynchronously on a given scheduler.
        /// </summary>
        public void RunAsync(Action action, Byte schedId = Byte.MaxValue) {
            _dbSession.RunAsync(action, schedId);
        }

        /// <summary>
        /// Runs a task asynchronously on current scheduler.
        /// </summary>
        public void RunSync(Action action, Byte schedId = Byte.MaxValue) {
            _dbSession.RunSync(action, schedId);
        }

        /// <summary>
        /// Updating last active session time tick.
        /// </summary>
        public void UpdateLastActive()
        {
            // Setting last active time.
            LastActiveTimeTick = GlobalSessions.AllGlobalSessions.GetSchedulerSessions(session_struct_.schedulerId_).CurrentTimeTick;
            LastActive = DateTime.UtcNow;
        }

        /// <summary>
        /// Prefix to data location URI.
        /// </summary>
        public static string DataLocationUriPrefix = "/__" + StarcounterEnvironment.DatabaseNameLower + "/";

        /// <summary>
        /// Escaped data location prefix.
        /// </summary>
        public static string DataLocationUriPrefixEscaped = "%2F__" + StarcounterEnvironment.DatabaseNameLower + "%2F";

        // Is being used.
        public Boolean IsBeingUsed()
        {
            if (apps_session_int_ != null)
                return apps_session_int_.IsBeingUsed();

            return false;
        }

        // Serializing session structure to bytes.
        internal void SerializeToBytes()
        {
            session_struct_.SerializeToBytes(session_bytes_);
        }

        // Checks if session is alive.
        public Boolean IsAlive()
        {
            return session_struct_.IsAlive();
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

            CargoId = 0;
        }

        // Session stored in ASCII bytes.
        Byte[] session_bytes_ = new Byte[MixedCodeConstants.SESSION_STRING_LEN_CHARS];

        // Session string representation.
        String session_string_ = null;

        // Converts internal bytes to session.
        public String ToAsciiString()
        {
            if (session_string_ == null)
                session_string_ = Encoding.ASCII.GetString(session_bytes_);

            return session_string_;
        }

        // Converts string to session.
        public void FromAsciiString(String sessionString)
        {
            session_string_ = sessionString;

            session_struct_.ParseFromString(session_string_);
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

        public ScSessionClass[] AppsSessions { get { return apps_sessions_; }}

        // List of free sessions.
        LinkedList<UInt32> free_session_indexes_ = new LinkedList<UInt32>();

        // List of used sessions.
        LinkedList<UInt32> used_session_indexes_ = new LinkedList<UInt32>();

        /// <summary>
        /// List of used session indexes.
        /// </summary>
        public LinkedList<UInt32> UsedSessionIndexes {
            get {
                return used_session_indexes_;
            }
        }

        /// <summary>
        /// Gets application session by index if its alive.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ScSessionClass GetAppsSessionIfAlive(UInt32 index) {

            // Getting session instance.
            ScSessionClass s = apps_sessions_[index];

            // Checking if session is created at all.
            if (s != null)
            {
                // Checking that session is active at all.
                if (s.session_struct_.IsAlive()) 
                {
                    // Checking that there is an Apps session at all.
                    if (s.apps_session_int_ != null)
                    {
                        // Checking that Apps session is not currently in use.
                        if (!s.apps_session_int_.IsBeingUsed()) 
                        {
                            return s;
                        }
                    }
                }
            }

            return null;
        }

        // Random generator for sessions.
        RNGCryptoServiceProvider rand_gen_ = new RNGCryptoServiceProvider();
        Byte[] rand_gen_buf_ = new Byte[8];

        // Current per-scheduler time tick.
        public UInt64 CurrentTimeTick = 0;

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
        internal UInt32 CreateNewSession(
            ref ScSessionStruct ss,
            IAppsSession apps_session_int)
        {
            // Getting free linear session index.
            LinkedListNode<UInt32> linear_index_node = free_session_indexes_.First;
            free_session_indexes_.RemoveFirst();

            // Obtaining linear index.
            ss.linearIndex_ = linear_index_node.Value;

            // Generating random salt.
            ss.randomSalt_ = GenerateRandomSalt();

            // Generating new session internally.
            return CreateNewSessionInternal(
                ref ss,
                apps_session_int,
                linear_index_node);
        }

        // Creates new Apps session.
        internal UInt32 CreateNewSessionInternal(
            ref ScSessionStruct ss,
            IAppsSession apps_session_int,
            LinkedListNode<UInt32> linear_index_node)
        {
            // Creating new session object if needed.
            if (apps_sessions_[ss.linearIndex_] == null)
                apps_sessions_[ss.linearIndex_] = new ScSessionClass();

            // Getting session class reference.
            ScSessionClass s = apps_sessions_[ss.linearIndex_];

            // Initializing session structure underneath.
            s.session_struct_.Init(
                ss.schedulerId_,
                ss.linearIndex_,
                ss.randomSalt_,
                ss.gwWorkerId_);

            // Serializing to bytes.
            s.SerializeToBytes();

            // Saving reference to internal session.
            if (apps_session_int != null)
                apps_session_int.InternalSession = apps_sessions_[ss.linearIndex_];

            // Setting last active time.
            s.UpdateLastActive();
            s.Created = DateTime.UtcNow;

            // Attaching the interface.
            s.apps_session_int_ = apps_session_int;

            // Attaching linear index node.
            s.linear_index_node_ = linear_index_node;

            // Adding to used sessions.
            used_session_indexes_.AddLast(linear_index_node);

            //s.session_struct_.PrintSession();

            return 0;
        }

        // Destroys existing Apps session.
        public UInt32 DestroySession(ScSessionStruct s)
        {
            return DestroySession(s.linearIndex_, s.randomSalt_);
        }

        // Destroys existing Apps session.
        public UInt32 DestroySession(UInt32 linear_index, UInt64 random_salt)
        {
            ScSessionClass s = apps_sessions_[linear_index];

            // Checking that session is created.
            if (null == s)
                return 0;

            // Checking that salt is correct.
            if (s.session_struct_.randomSalt_ == random_salt)
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
            if (linear_index >= MaxSessionsPerScheduler)
                return null;

            ScSessionClass s = apps_sessions_[linear_index];
            if (s == null)
                return null;

            // Checking for the correct session salt.
            if (random_salt == s.session_struct_.randomSalt_)
            {
                // Setting last active time.
                s.UpdateLastActive();

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
            if (linear_index >= MaxSessionsPerScheduler)
                return null;

            ScSessionClass s = apps_sessions_[linear_index];
            if (s == null)
                return null;

            // Checking for the correct session salt.
            if (random_salt == s.session_struct_.randomSalt_)
            {
                // Returning the session class.
                return s;
            }

            return null;
        }

        /// <summary>
        /// Looks up for inactive sessions and kills them. A Timer will schedule this method 
        /// as a job for each scheduler.
        /// </summary>
        public void InactiveSessionsCleanupRoutine() {

            try {

                // Incrementing global time.
                CurrentTimeTick++;

                UInt32 num_checked_sessions = 0;
                LinkedListNode<UInt32> used_session_index_node = used_session_indexes_.First;

                while (used_session_index_node != null) {

                    LinkedListNode<UInt32> next_used_session_index_node = used_session_index_node.Next;

                    // Getting session instance.
                    ScSessionClass s = GetAppsSessionIfAlive(used_session_index_node.Value);

                    // Checking if session is created at all.
                    if (s != null) {

                        // Checking if session is outdated.
                        if ((CurrentTimeTick - s.LastActiveTimeTick) > s.TimeoutMinutes + 1) {
                            // Destroying old session.
                            DestroySession(s.session_struct_);
                        }

                        num_checked_sessions++;
                    } else {
                        // NOTE: Apps session was destroyed already so deleting the wrapper.
                        DestroySession(s.session_struct_);
                    }

                    // Getting next used session.
                    used_session_index_node = next_used_session_index_node;

                    // Checking if we have scanned all created sessions.
                    if (num_checked_sessions >= used_session_indexes_.Count)
                        break;
                }

            } catch (Exception exc) {

                // Just logging the exception.
                Diagnostics.LogHostException(exc);
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

        public IAppsSession GetSession(string sessionId) {
            ScSessionStruct ssStruct = new ScSessionStruct();
            ssStruct.ParseFromString(sessionId);

            // Obtaining corresponding Apps session.
            IAppsSession apps_session =
                GlobalSessions.AllGlobalSessions.GetAppsSessionInterface(ref ssStruct);

            return apps_session;
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
        /// Creates a new session.
        /// </summary>
        /// <param name="apps_session"></param>
        /// <param name="scheduler_id"></param>
        /// <param name="session_index"></param>
        /// <param name="session_salt"></param>
        /// <returns></returns>
        public UInt32 CreateNewSession(
            ref ScSessionStruct ss,
            IAppsSession apps_session)
        {
            return scheduler_sessions_[ss.schedulerId_].CreateNewSession(ref ss, apps_session);
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
            return scheduler_sessions_[scheduler_id].DestroySession(linear_index, random_salt);
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
            return scheduler_sessions_[scheduler_id].GetSessionClass(linear_index, random_salt);
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
        /// <param name="reserved"></param>
        public delegate void CreateNewAppsSessionCallback(ref ScSessionStruct ss);
        
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
        public static void CreateNewSessionCallback(ref ScSessionStruct ss)
        {
            AllGlobalSessions.CreateNewSession(ref ss, null);
        }

        /// <summary>
        /// Returns existing Apps session interface.
        /// </summary>
        /// <param name="scheduler_id"></param>
        /// <param name="apps_session_index"></param>
        /// <returns></returns>
        internal IAppsSession GetAppsSessionInterface(ref ScSessionStruct ss)
        {
            return scheduler_sessions_[ss.schedulerId_].GetAppsSessionInterface(
                ss.linearIndex_,
                ss.randomSalt_);
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
        /// Currently active WebSocket.
        /// </summary>
        public WebSocket ActiveWebSocket { get; set; }

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