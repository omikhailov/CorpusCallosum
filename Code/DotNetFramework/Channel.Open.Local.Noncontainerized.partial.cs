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

namespace CorpusCallosum
{
    public abstract partial class Channel
    {
        /// <summary>
        /// Opens channel for writing. Channel must be created by process running without app container and it must be visible only from current user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static OperationResult<OutboundChannel> OpenOutboundLocalNoncontainerized(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to find shared memory channel");

            return OutboundChannel.Open(LifecycleHelper.LocalVisibilityPrefix + "\\" + name, name);
        }

        /// <summary>
        /// Opens channel for reading. Channel must be created by process running without app container and it must be visible only from current user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ObjectDoesNotExist or OperationStatus.AccessDenied
        /// </returns>
        public static OperationResult<InboundChannel> OpenInboundLocalNoncontainerized(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to find shared memory channel");

            return InboundChannel.Open(LifecycleHelper.LocalVisibilityPrefix + "\\" + name, name);
        }
    }
}
