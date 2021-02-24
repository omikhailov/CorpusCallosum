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
using System.Threading;
using System.Threading.Tasks;
using CorpusCallosum.SharedObjects;
using CorpusCallosum.SharedObjects.MemoryManagement;

namespace CorpusCallosum
{
    /// <summary>
    /// Base class for OutboundChannel and InboundChannel
    /// </summary>
    public abstract partial class Channel : IDisposable
    {
        public const long DefaultCapacity = 1024 * 1024 * 1024;

        protected bool _isClient;

        protected Semaphore _exclusiveAccessSemaphore;

        protected Semaphore _writerSemaphore;

        protected EventWaitHandle _hasMessagesEvent;

        protected EventWaitHandle _noMessagesEvent;

        protected EventWaitHandle _clientConnectedEvent;

        protected Semaphore _readerSemaphore;

        private protected MemoryManager _memoryManager;

        #region Instantiation

        internal Channel(string name, Semaphore writerSemaphore, Semaphore exclusiveAccessSemaphore, EventWaitHandle hasMessagesEvent, EventWaitHandle noMessagesEvent, EventWaitHandle clientConnectedEvent, Semaphore readerSemaphore)
        {
            Name = name;

            _writerSemaphore = writerSemaphore;

            _exclusiveAccessSemaphore = exclusiveAccessSemaphore;

            _hasMessagesEvent = hasMessagesEvent;

            _noMessagesEvent = noMessagesEvent;

            _clientConnectedEvent = clientConnectedEvent;

            _readerSemaphore = readerSemaphore;
        }

        #endregion

        /// <summary>
        /// Channel name. This property is thread-safe.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Maximum space allowed for allocation. This property is thread-safe.
        /// If writer will try to call Write() or WriteAsync when there is not enought free space, operation will return OperationStatus.OutOfSpace.
        /// Writer should call WaitWhenQueueIsEmpty() or await WhenQueueIsEmpty() to wait until reader will process all messages. 
        /// As an alternative, it can use GetChannelState() to track the number of active (not processed) messages in the queue.
        /// </summary>
        public long Capacity
        {
            get
            {
                ThrowIfDisposed();

                return _memoryManager.Capacity;
            }
        }

        /// <summary>
        /// The flag indicating that client is connected to this channel. This property is thread-safe.
        /// Server is the process that creates the channel (either inbound or outbound) and set access control rules for associated shared objects.
        /// Client is the process that opens it. When called by client, this property always returns true.
        /// </summary>
        public virtual bool IsClientConnected
        {
            get
            {
                return _isClient || WaitWhenClientConnected(TimeSpan.Zero) == OperationStatus.Completed;
            }
        }

        #region WaitWhenClientConnected

        /// <summary>
        /// Synchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        public void WaitWhenClientConnected()
        {
            WaitWhenClientConnected(Timeout.InfiniteTimeSpan);
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
            ThrowIfDisposed();

            if (_isClient) return OperationStatus.Completed;

            var completed = _clientConnectedEvent.WaitOne(timeout);

            return completed ? OperationStatus.Completed : OperationStatus.Timeout;
        }

        /// <summary>
        /// Synchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OpertionStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public OperationStatus WaitWhenClientConnected(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return WaitWhenClientConnected(timeout);

            ThrowIfDisposed();

            if (_isClient) return OperationStatus.Completed;

            var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _clientConnectedEvent }, timeout);

            if (index == 0) return OperationStatus.Cancelled;

            if (index == WaitHandle.WaitTimeout) return OperationStatus.Timeout;

            return OperationStatus.Completed;
        }

        /// <summary>
        /// Asynchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        public async Task WhenClientConnectedAsync()
        {
            await WhenClientConnectedAsync(Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Asynchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OpertionStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public async Task<OperationStatus> WhenClientConnectedAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();

            if (_isClient) return OperationStatus.Completed;

            return await AsyncHelper.GetTaskForWaitHandle(_clientConnectedEvent, timeout);
        }

        /// <summary>
        /// Asynchronously waits when client will open this channel. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OpertionStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public async Task<OperationStatus> WhenClientConnectedAsync(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await WhenClientConnectedAsync(timeout);

            ThrowIfDisposed();

            if (_isClient) return OperationStatus.Completed;

            return await AsyncHelper.GetTaskForWaitHandle(_clientConnectedEvent, cancellationToken, timeout);
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
            var operationResult = GetChannelState(Timeout.InfiniteTimeSpan);

            return operationResult.Data;
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation Timeout.</param>
        /// <returns>
        /// OperationResult with OperationStatus.Completed or OperationStatus.Timeout as well as ChannelState indicating current number of active messages in the queue
        /// </returns>
        public OperationResult<ChannelState> GetChannelState(TimeSpan timeout)
        {
            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                gainedExclusiveAccess = _exclusiveAccessSemaphore.WaitOne(timeout);

                if (!gainedExclusiveAccess) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);

                return new OperationResult<ChannelState>(OperationStatus.Completed, _memoryManager.GetChannelState());
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation Timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// OperationResult with OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled as well as ChannelState 
        /// indicating current number of active messages in the queue
        /// </returns>
        public OperationResult<ChannelState> GetChannelState(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return GetChannelState(timeout);

            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _exclusiveAccessSemaphore }, timeout);

                if (index == 0) return new OperationResult<ChannelState>(OperationStatus.Cancelled, null);

                if (index == WaitHandle.WaitTimeout) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);

                gainedExclusiveAccess = true;

                return new OperationResult<ChannelState>(OperationStatus.Completed, _memoryManager.GetChannelState());
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <returns>
        /// ChannelState with current number of active messages in the queue
        /// </returns>
        public async Task<ChannelState> GetChannelStateAsync()
        {
            var operationResult = await GetChannelStateAsync(Timeout.InfiniteTimeSpan);

            return operationResult.Data;
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation Timeout.</param>
        /// <returns>
        /// OperationResult with OperationStatus.Completed or OperationStatus.Timeout as well as ChannelState indicating current number of active messages in the queue
        /// </returns>
        public async Task<OperationResult<ChannelState>> GetChannelStateAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();

            var gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;

                return new OperationResult<ChannelState>(OperationStatus.Completed, _memoryManager.GetChannelState());
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        /// <summary>
        /// Retrieves information about current state of the channel. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation Timeout.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// OperationResult with OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled as well as ChannelState 
        /// indicating current number of active messages in the queue
        /// </returns>
        public async Task<OperationResult<ChannelState>> GetChannelStateAsync(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await GetChannelStateAsync(timeout);

            ThrowIfDisposed();

            var gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, cancellationToken, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;

                return new OperationResult<ChannelState>(OperationStatus.Completed, _memoryManager.GetChannelState());
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        #endregion

        #region IDisposable

        protected abstract void ThrowIfDisposed();

        protected bool _isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (disposing)
                {
                    if (_isClient) try { _clientConnectedEvent.Reset(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(_clientConnectedEvent); _clientConnectedEvent = null;

                    LifecycleHelper.PlatformSpecificDispose(_readerSemaphore); _readerSemaphore = null;

                    LifecycleHelper.PlatformSpecificDispose(_writerSemaphore); _writerSemaphore = null;

                    LifecycleHelper.PlatformSpecificDispose(_exclusiveAccessSemaphore); _exclusiveAccessSemaphore = null;

                    LifecycleHelper.PlatformSpecificDispose(_hasMessagesEvent); _hasMessagesEvent = null;

                    LifecycleHelper.PlatformSpecificDispose(_noMessagesEvent); _noMessagesEvent = null;

                    _memoryManager?.Dispose(); _memoryManager = null;
                }
            }
        }

        /// <summary>
        /// Disposes shared objects associated with this channel.
        /// Most of the resources associated with the channel and messages in the queue will stay alive until both client and server
        /// will be disposed. It's ok to reconnect or recreate channel with the same parameters.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
