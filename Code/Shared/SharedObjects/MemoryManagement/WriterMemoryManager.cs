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
using System.Threading.Tasks;
using CorpusCallosum.SharedObjects.MemoryManagement.WritingOperations;

namespace CorpusCallosum.SharedObjects.MemoryManagement
{
    internal class WriterMemoryManager : MemoryManager, IDisposable
    {
        public WriterMemoryManager(MemoryMappedFile file, long capacity) : base(file)
        {
            Capacity = capacity;

            Format();
        }

        public WriterMemoryManager(MemoryMappedFile file) : base(file)
        {
            var header = Header.Read(_headerView);

            Capacity = header.Capacity;
        }

        public OperationResult<ChannelState> Write(WritingOperation writingOperation, long length)
        {
            var header = Header.Read(_headerView);

            var allocationResult = AllocateSpaceFromFreeList(length, ref header);

            if (allocationResult.Status != OperationStatus.Completed) return new OperationResult<ChannelState>(allocationResult.Status, new ChannelState(header));

            var newNodePosition = allocationResult.Data;

            if (newNodePosition < 0)
            {
                if (header.TotalSpace + _sizeOfNode + length > Capacity) return new OperationResult<ChannelState>(OperationStatus.OutOfSpace, new ChannelState(header));

                newNodePosition = header.TotalSpace;
            }

            return WriteNodeAndCommit(newNodePosition, length, writingOperation, ref header);
        }

        private OperationResult<ChannelState> WriteNodeAndCommit(long newNodeOffset, long length, WritingOperation writingOperation, ref Header header)
        {
            var result = writingOperation.Write(_file, newNodeOffset + _sizeOfNode, length);

            if (result == OperationStatus.Cancelled || result == OperationStatus.DelegateFailed)
            {
                ReturnSpaceToFreeList(newNodeOffset, length, ref header);
            }
            else
            {
                var newNode = new Node(-1, length);

                Node.Write(_file, newNodeOffset, ref newNode);

                if (header.TailNode >= 0)
                {
                    using (var tailView = _file.CreateViewAccessor(header.TailNode, _sizeOfNode))
                    {
                        var tail = Node.Read(tailView);

                        tail.Next = newNodeOffset;

                        Node.Write(tailView, ref tail);
                    }
                }

                header.TailNode = newNodeOffset;

                if (header.HeadNode < 0) header.HeadNode = newNodeOffset;

                var allocated = (newNodeOffset + _sizeOfNode + length) - header.TotalSpace;

                if (allocated > 0) header.TotalSpace += allocated;

                header.ActiveNodes += 1;
            }

            Header.Write(_headerView, ref header);

            return new OperationResult<ChannelState>(result, new ChannelState(header));
        }

        public async Task<OperationResult<ChannelState>> WriteAsync(AsynchronousWritingOperation writingOperation, long length)
        {
            var header = Header.Read(_headerView);

            var allocationResult = AllocateSpaceFromFreeList(length, ref header);

            if (allocationResult.Status != OperationStatus.Completed) return new OperationResult<ChannelState>(allocationResult.Status, new ChannelState(header));

            var newNodePosition = allocationResult.Data;

            if (newNodePosition < 0)
            {
                if (header.TotalSpace + _sizeOfNode + length > Capacity) return new OperationResult<ChannelState>(OperationStatus.OutOfSpace, new ChannelState(header));

                newNodePosition = header.TotalSpace;
            }

            return await WriteNodeAndCommitAsync(newNodePosition, length, writingOperation, header);
        }

        private async Task<OperationResult<ChannelState>> WriteNodeAndCommitAsync(long newNodeOffset, long length, AsynchronousWritingOperation writingOperation, Header header)
        {
            var status = await writingOperation.WriteAsync(_file, newNodeOffset + _sizeOfNode, length);

            if (status == OperationStatus.Cancelled || status == OperationStatus.DelegateFailed)
            {
                ReturnSpaceToFreeList(newNodeOffset, length, ref header);
            }
            else
            {
                var newNode = new Node(-1, length);

                Node.Write(_file, newNodeOffset, ref newNode);

                if (header.TailNode >= 0)
                {
                    using (var tailView = _file.CreateViewAccessor(header.TailNode, _sizeOfNode))
                    {
                        var tail = Node.Read(tailView);

                        tail.Next = newNodeOffset;

                        Node.Write(tailView, ref tail);
                    }
                }

                header.TailNode = newNodeOffset;

                if (header.HeadNode < 0) header.HeadNode = newNodeOffset;

                var allocated = (newNodeOffset + _sizeOfNode + length) - header.TotalSpace;

                if (allocated > 0) header.TotalSpace += allocated;

                header.ActiveNodes += 1;
            }

            Header.Write(_headerView, ref header);

            return new OperationResult<ChannelState>(status, new ChannelState(header));
        }

        protected virtual OperationResult<long> AllocateSpaceFromFreeList(long dataLength, ref Header header)
        {
            long result = -1;

            if (header.FreeListNode >= 0)
            {
                var currentNodeOffset = header.FreeListNode;

                Node currentNode;

                long previousNodeOffset = -1;

                Node previousNode = new Node(-1, 0);

                bool isTheMostRightNode;

                bool isExactMatch;

                bool isLargeNode;

                do
                {
                    currentNode = Node.Read(_file, currentNodeOffset);

                    isTheMostRightNode = currentNodeOffset + _sizeOfNode + currentNode.Length >= header.TotalSpace;

                    isExactMatch = currentNode.Length == dataLength;

                    isLargeNode = currentNode.Length + _sizeOfNode >= dataLength + 2 * _sizeOfNode;

                    if (isTheMostRightNode || isExactMatch || isLargeNode)
                    {
                        result = currentNodeOffset;
                    }
                    else
                    {
                        currentNodeOffset = currentNode.Next;

                        previousNode = currentNode;

                        previousNodeOffset = currentNodeOffset;
                    }
                }
                while (result < 0 && currentNode.Next >= 0);

                if (result < 0) return new OperationResult<long>(OperationStatus.Completed, result);

                if (isTheMostRightNode && currentNodeOffset + _sizeOfNode + dataLength > Capacity) return new OperationResult<long>(OperationStatus.OutOfSpace, -1);

                var newNextOffset = currentNode.Next;

                if (isLargeNode)
                {
                    var newNode = new Node(currentNode.Next, currentNode.Length - dataLength - _sizeOfNode);

                    var newNodeOffset = currentNodeOffset + _sizeOfNode + dataLength;

                    newNextOffset = newNodeOffset;

                    Node.Write(_file, newNodeOffset, ref newNode);
                }

                if (previousNodeOffset >= 0)
                {
                    previousNode.Next = newNextOffset;

                    Node.Write(_file, previousNodeOffset, ref previousNode);
                }
                else
                {
                    header.FreeListNode = newNextOffset;
                }
            }

            return new OperationResult<long>(OperationStatus.Completed, result);
        }

        public override bool Equals(object other)
        {
            var wmm = other as WriterMemoryManager;

            if (wmm == null) return false;

            return _file.Equals(wmm._file);
        }

        public override int GetHashCode()
        {
            return 0 ^ _file.GetHashCode();
        }
    }
}
