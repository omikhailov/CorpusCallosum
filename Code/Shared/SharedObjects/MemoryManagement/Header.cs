// MIT License
//
// Copyright (c) 2021 Oleg Mikhailov
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.IO.MemoryMappedFiles;

namespace CorpusCallosum.SharedObjects.MemoryManagement
{
    internal struct Header
    {
        public const long Size = 48;

        public Header(long capacity, long totalSpace, long activeNodes, long headNode, long tailNode, long freeListNode)
        {
            _capacity = capacity;

            _totalSpace = totalSpace;

            _activeNodes = activeNodes;

            _headNode = headNode;

            _tailNode = tailNode;

            _freeListNode = freeListNode;
        }

        private long _capacity; // backing fields in sructs are required to get around a bug in UWP compiler

        public long Capacity
        {
            get
            {
                return _capacity;
            }
        }

        private long _totalSpace;

        public long TotalSpace
        {
            get
            {
                return _totalSpace;
            }
            set
            {
                _totalSpace = value;
            }
        }

        private long _activeNodes;

        public long ActiveNodes
        {
            get
            {
                return _activeNodes;
            }
            set
            {
                _activeNodes = value;
            }
        }

        private long _headNode;

        public long HeadNode
        {
            get
            {
                return _headNode;
            }
            set
            {
                _headNode = value;
            }
        }

        private long _tailNode;

        public long TailNode
        {
            get
            {
                return _tailNode;
            }
            set
            {
                _tailNode = value;
            }
        }

        private long _freeListNode;

        public long FreeListNode
        {
            get
            {
                return _freeListNode;
            }
            set
            {
                _freeListNode = value;
            }
        }

        public static Header Read(MemoryMappedViewAccessor view, long offset = 0)
        {
#if DOTNETSTANDARD_1_3
            return new Header(
                view.ReadInt64(offset), 
                view.ReadInt64(offset + sizeof(long)), 
                view.ReadInt64(offset + 2 * sizeof(long)),
                view.ReadInt64(offset + 3 * sizeof(long)),
                view.ReadInt64(offset + 4 * sizeof(long)),
                view.ReadInt64(offset + 5 * sizeof(long)));
#else
            Header result;

            view.Read(offset, out result);

            return result;
#endif
        }

        public static void Write(MemoryMappedViewAccessor view, ref Header header, long offset = 0)
        {
#if DOTNETSTANDARD_1_3
            view.Write(offset, header.Capacity);
    
            view.Write(offset + sizeof(long), header.TotalSpace);

            view.Write(offset + 2 * sizeof(long), header.ActiveNodes);

            view.Write(offset + 3 * sizeof(long), header.HeadNode);
            
            view.Write(offset + 4 * sizeof(long), header.TailNode);

            view.Write(offset + 5 * sizeof(long), header.FreeListNode);
#else
            view.Write(offset, ref header);
#endif
        }
    }
}
