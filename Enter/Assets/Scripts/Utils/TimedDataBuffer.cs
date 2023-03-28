using UnityEngine;
using System;
using System.Collections.Generic;

namespace Enter.Utils {
    public class TimedDataBuffer<T> where T : IComparable<T> {
        public float Duration {get; private set; }

        Queue<Tuple<float, T>> data;
        RemovablePriorityQueue<T> minQueue, maxQueue;

        public TimedDataBuffer(float duration) {
            Duration = duration;
            data = new Queue<Tuple<float, T>>();
            minQueue = new RemovablePriorityQueue<T>();
            maxQueue = new RemovablePriorityQueue<T>(Comparer<T>.Create((a,b) => -a.CompareTo(b)));
        }

        public T Last() { 
            if (data.Count == 0) {
                Debug.LogWarning("NULL RETURNED because timed data buffer does not hold any data");
                return default(T);
            }
            return data.Peek().Item2; 
        }

        public T GetMin() {
            if (minQueue.Count == 0) {
                Debug.LogWarning("NULL RETURNED because timed data buffer does not hold any data");
                return default(T);
            }
            return minQueue.Peek();
        }

        public T GetMax() {
            if (maxQueue.Count == 0) {
                Debug.LogWarning("NULL RETURNED because timed data buffer does not hold any data");
                return default(T);
            }
            return maxQueue.Peek();
        }

        public void Push(T item) {
            data.Enqueue(new Tuple<float, T>(Time.time, item));
            minQueue.Enqueue(item);
            maxQueue.Enqueue(item);
        }
        
        void InternalUpdate() {
            Tuple<float, T> dataItem;
            while (data.Count > 0 && data.TryPeek(out dataItem) && dataItem.Item1 + Duration < Time.time) {
                (float time, T value) = data.Dequeue();
                minQueue.Remove(value);
                maxQueue.Remove(value);
            }
        }
    }
}
