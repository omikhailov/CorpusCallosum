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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorpusCallosum.SharedObjects;
using CorpusCallosum.SharedObjects.MemoryManagement;

using Internal = CorpusCallosum;

namespace CorpusCallosum.WinRT
{
    public static class Channel
    {
        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewOutboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static NewOutboundChannelOperationResult CreateOutboundLocal(string name)
        {
            return new NewOutboundChannelOperationResult(Internal.Channel.CreateOutboundLocal(name));
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <returns>
        /// NewOutboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static NewOutboundChannelOperationResult CreateOutboundLocal(string name, long capacity)
        {
            return new NewOutboundChannelOperationResult(Internal.Channel.CreateOutboundLocal(name, capacity));
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewInboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static NewInboundChannelOperationResult CreateInboundLocal(string name)
        {
            return new NewInboundChannelOperationResult(Internal.Channel.CreateInboundLocal(name));
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <returns>
        /// NewInboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static NewInboundChannelOperationResult CreateInboundLocal(string name, long capacity)
        {
            return new NewInboundChannelOperationResult(Internal.Channel.CreateInboundLocal(name, capacity));
        }

        /// <summary>
        /// Opens channel for writing. Channel must be created by process running without app container and it must be visible only from current user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewOutboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static NewOutboundChannelOperationResult OpenOutboundLocalNoncontainerized(string name)
        {
            return new NewOutboundChannelOperationResult(Internal.Channel.OpenOutboundLocalNoncontainerized(name));
        }

        /// <summary>
        /// Opens channel for reading. Channel must be created by process running without app container and it must be visible only from current user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewInboundChannelOperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static NewInboundChannelOperationResult OpenInboundLocalNoncontainerized(string name)
        {
            return new NewInboundChannelOperationResult(Internal.Channel.OpenInboundLocalNoncontainerized(name));
        }

        /// <summary>
        /// Opens channel for writing. Channel must be created by process running without app container and it must be visible from all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewOutboundChannelOperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static NewOutboundChannelOperationResult OpenOutboundGlobalNoncontainerized(string name)
        {
            return new NewOutboundChannelOperationResult(Internal.Channel.OpenOutboundGlobalNoncontainerized(name));
        }

        /// <summary>
        /// Opens channel for reading. Channel must be created by process running without app container and it must be visible from all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// NewInboundChannelOperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static NewInboundChannelOperationResult OpenInboundGlobalNoncontainerized(string name)
        {
            return new NewInboundChannelOperationResult(Internal.Channel.OpenInboundGlobalNoncontainerized(name));
        }
    }
}
