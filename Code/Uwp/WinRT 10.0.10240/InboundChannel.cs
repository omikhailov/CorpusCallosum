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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Internal = CorpusCallosum;

namespace CorpusCallosum.WinRT
{
    public sealed class InboundChannel
    {
        private Internal.InboundChannel _internal;

        internal InboundChannel(Internal.InboundChannel @internal)
        {
            _internal = @internal;
        }

        /// <summary>
        /// Channel name. This property is thread-safe.
        /// </summary>
        public string Name { get { return _internal.Name; } }

        /// <summary>
        /// Maximum space allowed for allocation. This property is thread-safe.
        /// If writer will try to call Write() or WriteAsync when there is not enought free space, operation will return OperationStatus.OutOfSpace.
        /// Writer should call WaitWhenQueueIsEmpty() or await WhenQueueIsEmpty() to wait until reader will process all messages. 
        /// As an alternative, it can use GetChannelState() to track the number of active (not processed) messages in the queue.
        /// </summary>
        public long Capacity { get { return _internal.Capacity; } }

        /// <summary>
        /// The flag indicating that client is connected to this channel. This property is thread-safe.
        /// Server is the process that creates the channel (either inbound or outbound) and set access control rules for associated shared objects.
        /// Client is the process that opens it. When called by client, this property always returns true.
        /// </summary>
        public bool IsClientConnected { get { return _internal.IsClientConnected; } }

        #region WaitWhenClientConnected

        /// <summary>
        /// Synchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        public void WaitWhenClientConnected()
        {
            _internal.WaitWhenClientConnected();
        }

        /// <summary>
        /// Synchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OpertionStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public OperationStatus WaitWhenClientConnected(TimeSpan timeout)
        {
            return (OperationStatus)_internal.WaitWhenClientConnected(timeout);
        }

        /// <summary>
        /// Asynchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// OpertionStatus.Completed or OperationStatus.Cancelled.
        /// </returns>
        public IAsyncOperation<OperationStatus> WhenClientConnectedAsync()
        {
            return AsyncInfo.Run(async cancellationToken => (OperationStatus)await _internal.WhenClientConnectedAsync(cancellationToken, Timeout.InfiniteTimeSpan));
        }

        /// <summary>
        /// Asynchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// OpertionStatus.Completed, OperationStatus.Cancelled or OperationStatus.Timeout.
        /// </returns>
        public IAsyncOperation<OperationStatus> WhenClientConnectedAsync(TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken => (OperationStatus)await _internal.WhenClientConnectedAsync(cancellationToken, timeout));
        }

        #endregion

        #region GetChannelState

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// ChannelState with current number of active messages in the queue
        /// </returns>
        public ChannelState GetChannelState()
        {
            return new ChannelState(_internal.GetChannelState());
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation Timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with OperationStatus.Completed or OperationStatus.Timeout as well as ChannelState indicating current number of active messages in the queue
        /// </returns>
        public ChannelStateOperationResult GetChannelState(TimeSpan timeout)
        {
            return new ChannelStateOperationResult(_internal.GetChannelState(timeout));
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// ChannelStateOperationResult with OperationStatus.Completed or OperationStatus.Cancelled as well as ChannelState indicating current number of active messages in the queue
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> GetChannelStateAsync()
        {
            return AsyncInfo.Run(async cancellationToken => new ChannelStateOperationResult(await _internal.GetChannelStateAsync(cancellationToken, Timeout.InfiniteTimeSpan)));
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// ChannelStateOperationResult with OperationStatus.Completed, OperationStatus.Cancelled or OperationStatus.Timeout as well as ChannelState indicating current number of active messages in the queue
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> GetChannelStateAsync(TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken => new ChannelStateOperationResult(await _internal.GetChannelStateAsync(cancellationToken, timeout)));
        }

        #endregion

        #region Read

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public ChannelStateOperationResult Read(SynchronousReadingDelegate readingDelegate)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate)));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, 
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public ChannelStateOperationResult Read(SynchronousReadingDelegate readingDelegate, TimeSpan timeout)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate), timeout));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        [DefaultOverload]
        public ChannelStateOperationResult Read(SynchronousReadingDelegateWithResult readingDelegate)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate)));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        [DefaultOverload]
        public ChannelStateOperationResult Read(SynchronousReadingDelegateWithResult readingDelegate, TimeSpan timeout)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate), timeout));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public ChannelStateOperationResult Read(SynchronousReadingDelegateWithParameterAndResult readingDelegate, object parameter)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate), parameter));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public ChannelStateOperationResult Read(SynchronousReadingDelegateWithParameterAndResult readingDelegate, object parameter, TimeSpan timeout)
        {
            return new ChannelStateOperationResult(_internal.Read(DelegateHelper.Wrap(readingDelegate), parameter, timeout));
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegate readingDelegate)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), null, cancellationToken, Timeout.InfiniteTimeSpan));
            });
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled, 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegate readingDelegate, TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), null, cancellationToken, timeout));
            });
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        [DefaultOverload]
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegateWithResult readingDelegate)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), null, cancellationToken, Timeout.InfiniteTimeSpan));
            });
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled, 
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        [DefaultOverload]
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegateWithResult readingDelegate, TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), null, cancellationToken, timeout));
            });
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled,
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegateWithParameterAndResult readingDelegate, object parameter)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), parameter, cancellationToken, Timeout.InfiniteTimeSpan));
            });
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// ChannelStateOperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled, 
        /// OperationStatus.DelegateFailed, OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public IAsyncOperation<ChannelStateOperationResult> ReadAsync(AsynchronousReadingDelegateWithParameterAndResult readingDelegate, object parameter, TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken =>
            {
                return new ChannelStateOperationResult(await _internal.ReadAsync(DelegateHelper.Wrap(readingDelegate), parameter, cancellationToken, timeout));
            });
        }

        #endregion

        #region WaitWhenQueueHasMessages

        /// <summary>
        /// Synchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        public void WaitWhenQueueHasMessages()
        {
            _internal.WaitWhenQueueHasMessages();
        }

        /// <summary>
        /// Synchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public OperationStatus WaitWhenQueueHasMessages(TimeSpan timeout)
        {
            return (OperationStatus)_internal.WaitWhenQueueHasMessages(timeout);
        }

        /// <summary>
        /// Asynchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// OperationStatus.Completed or OperationStatus.Cancelled.
        /// </returns>
        public IAsyncOperation<OperationStatus> WhenQueueHasMessagesAsync()
        {
            return AsyncInfo.Run(async cancellationToken => (OperationStatus)(await _internal.WhenQueueHasMessagesAsync(cancellationToken, Timeout.InfiniteTimeSpan)));
        }

        /// <summary>
        /// Asynchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed, OperationStatus.Cancelled or OperationStatus.Timeout.
        /// </returns>
        public IAsyncOperation<OperationStatus> WhenQueueHasMessagesAsync(TimeSpan timeout)
        {
            return AsyncInfo.Run(async cancellationToken => (OperationStatus)(await _internal.WhenQueueHasMessagesAsync(cancellationToken, timeout)));
        }

        #endregion


        #region IDisposable

        /// <summary>
        /// Disposes shared objects associated with this channel.
        /// Most of the resources associated with the channel and messages in the queue will stay alive until both client and server
        /// will be disposed. It's ok to reconnect or recreate channel with the same parameters.
        /// </summary>
        public void Dispose()
        {
            _internal?.Dispose();

            _internal = null;
        }

        #endregion

        #region Object

        public override bool Equals(object obj)
        {
            var adapter = obj as InboundChannel;

            if (adapter == null) return false;

            return _internal.Equals(adapter._internal);
        }

        public override int GetHashCode()
        {
            return _internal.GetHashCode();
        }

        #endregion
    }
}
