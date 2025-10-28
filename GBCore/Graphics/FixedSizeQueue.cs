using System;
using System.Collections.Generic;

namespace GBCore.Graphics
{
    public class FixedSizeQueue
    {
        private readonly int _maxSize = 0;
        private Queue<int> _queue;

        public FixedSizeQueue(int maxSize)
        {
            _maxSize = maxSize;
            _queue = new Queue<int>();
        }

        public void Enqueue(int value)
        {
            if (_queue.Count >= _maxSize)
            {
                throw new Exception("Queue full");
            }
            _queue.Enqueue(value);
        }

        public int Dequeue()
        {
            if (_queue.Count == 0)
            {
                return -1;
            }
            return _queue.Dequeue();
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public int Size()
        {
            return _queue.Count;
        }
    }
}