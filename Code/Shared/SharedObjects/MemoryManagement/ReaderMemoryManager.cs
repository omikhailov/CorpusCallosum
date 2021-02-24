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
using CorpusCallosum.SharedObjects.MemoryManagement.ReadingOperations;

namespace CorpusCallosum.SharedObjects.MemoryManagement
{
    internal class ReaderMemoryManager : MemoryManager, IDisposable
    {
        public ReaderMemoryManager(MemoryMappedFile file) : base(file) 
        {
            var header = Header.Read(_headerView);

            Capacity = header.Capacity;
        }

        public ReaderMemoryManager(MemoryMappedFile file, long capacity) : base(file) 
        {
            Capacity = capacity;

            Format();
        }

        public OperationResult<ChannelState> Read(ReadingOperation readingOperation)
        {
            var header = Header.Read(_headerView);

            if (header.HeadNode < 0) return new OperationResult<ChannelState>(OperationStatus.QueueIsEmpty, new ChannelState(header));

            var offset = header.HeadNode;

            var node = Node.Read(_file, header.HeadNode);

            var status = readingOperation.Read(_file, header.HeadNode + _sizeOfNode, node.Length);

            if (status == OperationStatus.Cancelled || status == OperationStatus.DelegateFailed) return new OperationResult<ChannelState>(status, new ChannelState(header));

            header.HeadNode = node.Next;

            header.ActiveNodes -= 1;

            ReturnSpaceToFreeList(offset, _sizeOfNode + node.Length, ref header);

            Header.Write(_headerView, ref header);

            return new OperationResult<ChannelState>(status, new ChannelState(header));
        }

        public async Task<OperationResult<ChannelState>> ReadAsync(AsynchronousReadingOperation readingOperation)
        {
            var header = Header.Read(_headerView);

            if (header.HeadNode < 0) return new OperationResult<ChannelState>(OperationStatus.QueueIsEmpty, new ChannelState(header));

            var offset = header.HeadNode;

            var node = Node.Read(_file, header.HeadNode);

            var status = await readingOperation.ReadAsync(_file, header.HeadNode + _sizeOfNode, node.Length);

            if (status == OperationStatus.Cancelled || status == OperationStatus.DelegateFailed) return new OperationResult<ChannelState>(status, new ChannelState(header));

            header.HeadNode = node.Next;

            header.ActiveNodes -= 1;

            ReturnSpaceToFreeList(offset, _sizeOfNode + node.Length, ref header);

            Header.Write(_headerView, ref header);

            return new OperationResult<ChannelState>(status, new ChannelState(header));
        }
        
        public override bool Equals(object other)
        {
            var rmm = other as ReaderMemoryManager;

            if (rmm == null) return false;

            return _file.Equals(rmm._file);
        }

        public override int GetHashCode()
        {
            return -1 ^ _file.GetHashCode();
        }
    }
}
