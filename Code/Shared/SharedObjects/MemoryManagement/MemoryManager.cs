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

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace CorpusCallosum.SharedObjects.MemoryManagement
{
    internal abstract class MemoryManager : IDisposable
    {
        protected MemoryMappedFile _file;

        protected MemoryMappedViewAccessor _headerView;
        
        protected int _sizeOfHeader;

        protected int _sizeOfNode;

        public MemoryManager(MemoryMappedFile file)
        {
            _file = file;

            _sizeOfHeader = Marshal.SizeOf<Header>();

            _sizeOfNode = Marshal.SizeOf<Node>();

            _headerView = file.CreateViewAccessor(0, _sizeOfHeader);
        }

        public long Capacity { get; protected set; }

        public void Format()
        {
            var header = new Header(Capacity, _sizeOfHeader, 0, -1, -1, -1);

            Header.Write(_headerView, ref header);
        }

        public ChannelState GetChannelState()
        {
            var header = Header.Read(_headerView);

            return new ChannelState(header);
        }

        protected virtual void ReturnSpaceToFreeList(long offset, long length, ref Header header)
        {
            var nextNodeOffset = header.FreeListNode;

            var previousNodeOffset = (long)-1;

            Node nextNode = new Node(-1, 0);

            Node previousNode = new Node(-1, 0);

            while (nextNodeOffset >= 0)
            {
                nextNode = Node.Read(_file, nextNodeOffset);

                if (nextNodeOffset > offset) break;

                previousNodeOffset = nextNodeOffset;

                previousNode = nextNode;

                nextNodeOffset = nextNode.Next;
            }

            var canJoinWithPrevious = previousNodeOffset >= 0 && previousNodeOffset + previousNode.Length == offset;

            var canJoinWithNext = nextNodeOffset >= 0 && offset + length == nextNodeOffset;

            if (canJoinWithPrevious && canJoinWithNext)
            {
                previousNode.Length += length + _sizeOfNode + nextNode.Length;

                previousNode.Next = nextNode.Next;

                Node.Write(_file, previousNodeOffset, ref previousNode);
            }
            else
            if (canJoinWithPrevious)
            {
                previousNode.Length += length;

                Node.Write(_file, previousNodeOffset, ref previousNode);
            }
            else
            if (canJoinWithNext)
            {
                var newNode = new Node(nextNode.Next, length + nextNode.Length);

                Node.Write(_file, offset, ref newNode);

                if (previousNodeOffset < 0)
                {
                    header.FreeListNode = offset;
                }
                else
                {
                    previousNode.Next = offset;

                    Node.Write(_file, previousNodeOffset, ref previousNode);
                }
            }
            else
            {
                var newNode = new Node(nextNodeOffset, length - _sizeOfNode);

                Node.Write(_file, offset, ref newNode);

                if (previousNodeOffset < 0)
                {
                    header.FreeListNode = offset;
                }
                else
                {
                    previousNode.Next = offset;

                    Node.Write(_file, previousNodeOffset, ref previousNode);
                }
            }
        }

        #region IDisposable

        protected bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    _headerView?.Dispose(); _headerView = null;

                    _file?.Dispose(); _file = null;
                }

                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
