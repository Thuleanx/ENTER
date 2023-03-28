using System;
using System.Collections;
using System.Collections.Generic;

// Using outside code will have to rewrite later
namespace Enter.Utils {
	public class RemovablePriorityQueue<T> where T : IComparable<T>
    {
        private readonly BHeap<T> heapA;
        private readonly BHeap<T> heapB;
        bool resolveNeeded = false;

        public RemovablePriorityQueue()
        {
            heapA = new BHeap<T>();
            heapB = new BHeap<T>();
        }

		public RemovablePriorityQueue(IComparer<T> comparer) {
			heapA = new BHeap<T>(comparer);
            heapB = new BHeap<T>(comparer);
		}

        /// <summary>
        /// Time complexity:O(log(n)).
        /// </summary>
        public void Enqueue(T item)
        {
            if (resolveNeeded) Resolve();
            heapA.Insert(item);
        }

        /// <summary>
        /// Time complexity:O(log(n)).
        /// </summary>
        public T Dequeue()
        {
            resolveNeeded = true;
            return heapA.Extract();
        }

        /// <summary>
        ///  Remove something from the queue. Undefined behaviour if it is not present in queue.
        /// </summary>
        public void Remove(T item)
        {
            resolveNeeded = true;
            heapB.Insert(item);
        }

        /// <summary>
        /// Time complexity:O(1).
        /// </summary>
        public T Peek()
        {
            if (resolveNeeded) Resolve();
            return heapA.Peek();
        }

		public int Count => heapA.Count - heapB.Count;

        private void Resolve() {
            while (heapA.Count > 0 && heapB.Count > 0 && EqualityComparer<T>.Default.Equals(heapA.Peek(), heapB.Peek())) {
                heapA.Extract();
                heapB.Extract();
            }
            resolveNeeded = false;
        }
    }
}
