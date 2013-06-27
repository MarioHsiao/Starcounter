using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Starcounter.Internal
{
    public static class SyncMethods
    {

        public static bool CAS<T>(ref T location, T comparand, T newValue) where T : class
        {
            return
                (object)comparand ==
                (object)Interlocked.CompareExchange<T>(ref location, newValue, comparand);
        }
    }

    internal class SingleLinkNode<T>
    {
        // Note; the Next member cannot be a property since it participates in
        // many CAS operations
        public SingleLinkNode<T> Next;
        public T Item;
    }

    public class LockFreeStack<T>
    {

        private SingleLinkNode<T> head;

        public LockFreeStack()
        {
            head = new SingleLinkNode<T>();
        }

        public void Push(T item)
        {
            SingleLinkNode<T> newNode = new SingleLinkNode<T>();
            newNode.Item = item;
            do
            {
                newNode.Next = head.Next;
            } while (!SyncMethods.CAS<SingleLinkNode<T>>(ref head.Next, newNode.Next, newNode));
        }

        public bool Pop(out T item)
        {
            SingleLinkNode<T> node;
            do
            {
                node = head.Next;
                if (node == null)
                {
                    item = default(T);
                    return false;
                }
            } while (!SyncMethods.CAS<SingleLinkNode<T>>(ref head.Next, node, node.Next));
            item = node.Item;
            return true;
        }

        public T Pop()
        {
            T result;
            Pop(out result);
            return result;
        }
    }
}
