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
using CorpusCallosum.SharedObjects;
using CorpusCallosum.SharedObjects.MemoryManagement;

namespace CorpusCallosum
{
    public abstract partial class Channel
    {
        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name)
        {
            return CreateOutboundLocal(name, DefaultCapacity);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name, long capacity)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be at least {Header.Size} bytes");

            return OutboundChannel.Create(LifecycleHelper.LocalVisibilityPrefix + "\\" + name, name, capacity, null);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name)
        {
            return CreateInboundLocal(name, DefaultCapacity);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name, long capacity)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be greater than {Header.Size} bytes");

            return InboundChannel.Create(LifecycleHelper.LocalVisibilityPrefix + "\\" +  name, name, capacity, null);
        }
    }
}
