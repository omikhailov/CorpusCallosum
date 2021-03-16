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

namespace CorpusCallosum.WinRT
{
    /// <summary>
    /// Enum indicating operation completion status
    /// </summary>
    public enum OperationStatus
    {
        /// <summary>
        /// Operation completed.
        /// </summary>
        Completed,
        /// <summary>
        /// Shared memory and synchronization objects associated with this channel name are already in use by another process or channel was not properly disposed after last use.
        /// </summary>
        ObjectAlreadyInUse,
        /// <summary>
        /// The channel with the specified name does not exist.
        /// </summary>
        ObjectDoesNotExist,
        /// <summary>
        /// Cannot create channel because memory mapped file cannot be as large as specified capacity.
        /// </summary>
        CapacityIsGreaterThanLogicalAddressSpace,
        /// <summary>
        /// Current user does not have permissions to create global named objects. Administrative accounts, LocalService and NetworkService have this permission out of the box.
        /// </summary>
        ElevationRequired,
        /// <summary>
        /// Channel exists, but process serving it did not grant access to this client. To access channel from UWP app container, generate container's SID and add it to ACL when you create the channel.
        /// </summary>
        AccessDenied,
        /// <summary>
        /// Requested message length is too large.
        /// </summary>
        RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// <summary>
        /// Requested message length is too large.
        /// </summary>
        RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// <summary>
        /// There are no messages in the queue.
        /// </summary>
        QueueIsEmpty,
        /// <summary>
        /// Time is up and the operation was not performed. During this time, other operations continued to execute exclusively, or the expected event did not occur. 
        /// </summary>
        Timeout,
        /// <summary>
        /// Operation was cancelled.
        /// </summary>
        Cancelled,
        /// <summary>
        /// Memory mapped file capacity limit reached and message with specified length cannot be added to the queue right now. You can try to write it later.
        /// </summary>
        OutOfSpace,
        /// <summary>
        /// Delegate threw an exception and operation wasn't completed. When it happens, writing operations don't add new message to the queue, reading operations don't remove it.
        /// </summary>
        DelegateFailed
    }
}
