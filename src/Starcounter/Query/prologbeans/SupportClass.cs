// Copyright (c) 2004 SICS AB. All rights reserved.

namespace se.sics.prologbeans
{
using System;
/*******************************/
/// <summary>
/// Internal interface for PrologBeans.
/// </summary>
public interface IThreadRunnable
{
    /// <summary>
    /// </summary>
    void Run();
}

/*******************************/
/// <summary>
/// Internal class for PrologBeans.
/// </summary>
public class SupportClass
{
    /// <summary>
    /// </summary>
    public static System.Object PutElement(System.Collections.Hashtable hashTable, System.Object key, System.Object newValue)
    {
        System.Object element = hashTable[key];
        hashTable[key] = newValue;
        return element;
    }

    /*******************************/
    /// <summary>
    /// This method is used as a dummy method to simulate VJ++ behavior
    /// </summary>
    /// <param name="literal">The literal to return</param>
    /// <returns>The received value</returns>
    public static long Identity(long literal)
    {
        return literal;
    }

    /// <summary>
    /// This method is used as a dummy method to simulate VJ++ behavior
    /// </summary>
    /// <param name="literal">The literal to return</param>
    /// <returns>The received value</returns>
    public static ulong Identity(ulong literal)
    {
        return literal;
    }

    /// <summary>
    /// This method is used as a dummy method to simulate VJ++ behavior
    /// </summary>
    /// <param name="literal">The literal to return</param>
    /// <returns>The received value</returns>
    public static float Identity(float literal)
    {
        return literal;
    }

    /// <summary>
    /// This method is used as a dummy method to simulate VJ++ behavior
    /// </summary>
    /// <param name="literal">The literal to return</param>
    /// <returns>The received value</returns>
    public static double Identity(double literal)
    {
        return literal;
    }

    /*******************************/
    /// <summary>
    /// Converts an array of sbytes to an array of bytes
    /// </summary>
    /// <param name="sbyteArray">The array of sbytes to be converted</param>
    /// <returns>The new array of bytes</returns>
    public static byte[] ToByteArray(sbyte[] sbyteArray)
    {
        byte[] byteArray = new byte[sbyteArray.Length];
        for (int index = 0; index < sbyteArray.Length; index++)
        {
            byteArray[index] = (byte) sbyteArray[index];
        }
        return byteArray;
    }

    /// <summary>
    /// Converts a string to an array of bytes
    /// </summary>
    /// <param name="sourceString">The string to be converted</param>
    /// <returns>The new array of bytes</returns>
    public static byte[] ToByteArray(string sourceString)
    {
        byte[] byteArray = new byte[sourceString.Length];
        for (int index = 0; index < sourceString.Length; index++)
        {
            byteArray[index] = (byte) sourceString[index];
        }
        return byteArray;
    }

    /*******************************/
    /// <summary>
    /// </summary>
    public static sbyte[] ToSByteArray(byte[] byteArray)
    {
        sbyte[] sbyteArray = new sbyte[byteArray.Length];
        for (int index = 0; index < byteArray.Length; index++)
        {
            sbyteArray[index] = (sbyte) byteArray[index];
        }
        return sbyteArray;
    }

    /*******************************/
    /// <summary>
    /// Internal class for PrologBeans.
    /// </summary>
    public class ThreadClass: IThreadRunnable
    {
        private System.Threading.Thread threadField;

        /// <summary>
        /// </summary>
        public ThreadClass()
        {
            threadField = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
        }

        /// <summary>
        /// </summary>
        public ThreadClass(System.Threading.ThreadStart p1)
        {
            threadField = new System.Threading.Thread(p1);
        }

        /// <summary>
        /// </summary>
        public virtual void Run()
        {
        }

        /// <summary>
        /// </summary>
        public virtual void Start()
        {
            threadField.Start();
        }

        /// <summary>
        /// </summary>
        public System.Threading.Thread Instance
        {
            get
            {
                return threadField;
            }
            set
            {
                threadField = value;
            }
        }

        /// <summary>
        /// </summary>
        public System.String Name
        {
            get
            {
                return threadField.Name;
            }
            set
            {
                if (threadField.Name == null)
                {
                    threadField.Name = value;
                }
            }
        }

        /// <summary>
        /// </summary>
        public System.Threading.ThreadPriority Priority
        {
            get
            {
                return threadField.Priority;
            }
            set
            {
                threadField.Priority = value;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return threadField.IsAlive;
            }
        }

        /// <summary>
        /// </summary>
        public bool IsBackground
        {
            get
            {
                return threadField.IsBackground;
            }
            set
            {
                threadField.IsBackground = value;
            }
        }

        /// <summary>
        /// </summary>
        public void Join()
        {
            threadField.Join();
        }

        /// <summary>
        /// </summary>
        public void Join(long p1)
        {
            lock (this)
            {
                threadField.Join(new System.TimeSpan(p1 * 10000));
            }
        }

        /// <summary>
        /// </summary>
        public void Join(long p1, int p2)
        {
            lock (this)
            {
                threadField.Join(new System.TimeSpan(p1 * 10000 + p2 * 100));
            }
        }

        // [PD] 3.12.3+
        //      System.Threading.Thread.Resume()' is obsolete in .NET 2.0
        //    /// <summary>
        //    /// </summary>
        //    public void Resume()
        //      {
        //        threadField.Resume();
        //      }

        /// <summary>
        /// </summary>
        public void Abort()
        {
            threadField.Abort();
        }

        /// <summary>
        /// </summary>
        public void Abort(System.Object stateInfo)
        {
            lock (this)
            {
                threadField.Abort(stateInfo);
            }
        }

        // [PD] 3.12.3+
        //      System.Threading.Thread.Suspend()' is obsolete in .NET 2.0
        //    /// <summary>
        //    /// </summary>
        //    public void Suspend()
        //      {
        //        threadField.Suspend();
        //      }

        /// <summary>
        /// </summary>
        public override System.String ToString()
        {
            return "Thread[" + Name + "," + Priority.ToString() + "," + "" + "]";
        }

        /// <summary>
        /// </summary>
        public static ThreadClass Current()
        {
            ThreadClass CurrentThread = new ThreadClass();
            CurrentThread.Instance = System.Threading.Thread.CurrentThread;
            return CurrentThread;
        }
    }

    /*******************************/
    /// <summary>
    /// </summary>
    public static void WriteStackTrace(System.Exception throwable, System.IO.TextWriter stream)
    {
        stream.WriteLine(throwable.ToString()); // [PD] 3.12.6
        stream.Write(throwable.StackTrace);
        stream.Flush();
    }

    /*******************************/
    /// <summary>
    /// Copies an array of chars obtained from a String into a specified array of chars
    /// </summary>
    /// <param name="sourceString">The String to get the chars from</param>
    /// <param name="sourceStart">Position of the String to start getting the chars</param>
    /// <param name="sourceEnd">Position of the String to end getting the chars</param>
    /// <param name="destinationArray">Array to return the chars</param>
    /// <param name="destinationStart">Position of the destination array of chars to start storing the chars</param>
    /// <returns>An array of chars</returns>
    public static void GetCharsFromString(string sourceString, int sourceStart, int sourceEnd, ref char[] destinationArray, int destinationStart)
    {
        int sourceCounter;
        int destinationCounter;
        sourceCounter = sourceStart;
        destinationCounter = destinationStart;
        while (sourceCounter < sourceEnd)
        {
            destinationArray[destinationCounter] = (char) sourceString[sourceCounter];
            sourceCounter++;
            destinationCounter++;
        }
    }

}
}
