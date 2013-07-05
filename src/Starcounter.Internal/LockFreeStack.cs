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
        public Int32 Count;
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

            Count++;
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
            Count--;
            return true;
        }

        public T Pop()
        {
            T result;
            Pop(out result);
            return result;
        }
    }

    public class LockFreeQueue<T>
    {
        SingleLinkNode<T> head;
        SingleLinkNode<T> tail;
        public Int32 Count;

        public LockFreeQueue()
        {
            head = new SingleLinkNode<T>();
            tail = head;
        }

        public void Enqueue(T item)
        {
            SingleLinkNode<T> oldTail = null;
            SingleLinkNode<T> oldTailNext;

            SingleLinkNode<T> newNode = new SingleLinkNode<T>();
            newNode.Item = item;

            bool newNodeWasAdded = false;
            while (!newNodeWasAdded)
            {
                oldTail = tail;
                oldTailNext = oldTail.Next;

                if (tail == oldTail)
                {
                    if (oldTailNext == null)
                        newNodeWasAdded = SyncMethods.CAS<SingleLinkNode<T>>(ref tail.Next, null, newNode);
                    else
                        SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, oldTailNext);
                }
            }

            SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, newNode);

            Count++;
        }

        public bool Dequeue(out T item)
        {
            item = default(T);
            SingleLinkNode<T> oldHead = null;

            bool haveAdvancedHead = false;
            while (!haveAdvancedHead)
            {
                oldHead = head;
                SingleLinkNode<T> oldTail = tail;
                SingleLinkNode<T> oldHeadNext = oldHead.Next;

                if (oldHead == head)
                {
                    if (oldHead == oldTail)
                    {
                        if (oldHeadNext == null)
                        {
                            return false;
                        }
                        SyncMethods.CAS<SingleLinkNode<T>>(ref tail, oldTail, oldHeadNext);
                    }

                    else
                    {
                        item = oldHeadNext.Item;
                        haveAdvancedHead =
                          SyncMethods.CAS<SingleLinkNode<T>>(ref head, oldHead, oldHeadNext);
                    }
                }
            }

            Count--;

            return true;
        }

        public T Dequeue()
        {
            T result;
            Dequeue(out result);
            return result;
        }
    }
}
