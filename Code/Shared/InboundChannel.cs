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
using System.Threading;
using System.Threading.Tasks;
using CorpusCallosum.SharedObjects;
using CorpusCallosum.SharedObjects.MemoryManagement;
using CorpusCallosum.SharedObjects.MemoryManagement.ReadingOperations;

#if !UWP
using Acl = System.Collections.Generic.IEnumerable<System.Security.Principal.IdentityReference>;
#endif

namespace CorpusCallosum
{
    /// <summary>
    /// Inbound channel to read messages from the queue.
    /// </summary>
    public sealed class InboundChannel : Channel, IDisposable
    {
        #region Instantiation

        internal InboundChannel(string name, Semaphore readerSemaphore, Semaphore exclusiveAccessSemaphore, MemoryMappedFile file, long capacity,
            EventWaitHandle hasMessagesEvent, EventWaitHandle noMessagesEvent, EventWaitHandle clientConnectedEvent, Semaphore writerSemaphore)
            : base(name, writerSemaphore, exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, readerSemaphore)
        {
            _isClient = false;            

            _memoryManager = new ReaderMemoryManager(file, capacity);
        }

        internal InboundChannel(string name, Semaphore readerSemaphore, Semaphore exclusiveAccessSemaphore, MemoryMappedFile file, 
            EventWaitHandle hasMessagesEvent, EventWaitHandle noMessagesEvent, EventWaitHandle clientConnectedEvent, Semaphore writerSemaphore)
            : base(name, writerSemaphore, exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, readerSemaphore)
        {
            _isClient = true;

            _memoryManager = new ReaderMemoryManager(file);            

            _clientConnectedEvent.Set();
        }

        internal static OperationResult<InboundChannel> Open(string fullName, string name)
        {
            Semaphore readerSemaphore = null;

            MemoryMappedFile file = null;

            Semaphore exclusiveAccessSemaphore = null;

            EventWaitHandle hasMessagesEvent = null;

            EventWaitHandle noMessagesEvent = null;

            EventWaitHandle clientConnectedEvent = null;

            try
            {
                var readerSemaphoreOperationResult = LifecycleHelper.OpenAndSetReaderSemaphore(fullName);

                if (readerSemaphoreOperationResult.Status != OperationStatus.Completed) return new OperationResult<InboundChannel>(readerSemaphoreOperationResult.Status, null);

                readerSemaphore = readerSemaphoreOperationResult.Data;

                var openCommonSharedObjectsStatus = LifecycleHelper.OpenCommonSharedObjects(fullName, out file, out exclusiveAccessSemaphore, out hasMessagesEvent, out noMessagesEvent, out clientConnectedEvent);

                if (openCommonSharedObjectsStatus != OperationStatus.Completed)
                {
                    try { readerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(readerSemaphore);
                }

                var result = new InboundChannel(name, readerSemaphore, exclusiveAccessSemaphore, file, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, null);

                return new OperationResult<InboundChannel>(OperationStatus.Completed, result);
            }
            catch
            {
                if (readerSemaphore != null)
                {
                    try { readerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(readerSemaphore);
                }

                LifecycleHelper.PlatformSpecificDispose(exclusiveAccessSemaphore);

                file?.Dispose();

                LifecycleHelper.PlatformSpecificDispose(hasMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(noMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(clientConnectedEvent);

                throw;
            }
        }

        internal static OperationResult<InboundChannel> Create(string fullName, string name, long capacity, Acl acl)
        {
            Semaphore readerSemaphore = null;

            MemoryMappedFile file = null;

            Semaphore exclusiveAccessSemaphore = null;

            EventWaitHandle hasMessagesEvent = null;

            EventWaitHandle noMessagesEvent = null;

            EventWaitHandle clientConnectedEvent = null;

            Semaphore writerSemaphore = null;

            try
            {
                var readerSemaphoreOperationResult = LifecycleHelper.CreateAndSetReaderSemaphore(fullName, acl);

                if (readerSemaphoreOperationResult.Status != OperationStatus.Completed) return new OperationResult<InboundChannel>(readerSemaphoreOperationResult.Status, null);
                
                readerSemaphore = readerSemaphoreOperationResult.Data;

                var fileOperationResult = LifecycleHelper.CreateMemoryMappedFile(fullName, capacity, acl);

                if (fileOperationResult.Status != OperationStatus.Completed)
                {
                    LifecycleHelper.PlatformSpecificDispose(readerSemaphore);

                    return new OperationResult<InboundChannel>(fileOperationResult.Status, null);
                }

                file = fileOperationResult.Data;

                exclusiveAccessSemaphore = LifecycleHelper.CreateExclusiveAccessSemaphore(fullName, acl);

                hasMessagesEvent = LifecycleHelper.CreateHasMessagesEvent(fullName, acl);

                noMessagesEvent = LifecycleHelper.CreateNoMessagesEvent(fullName, acl);

                clientConnectedEvent = LifecycleHelper.CreateClientConnectedEvent(fullName, acl);

                writerSemaphore = LifecycleHelper.CreateWriterSemaphore(fullName, acl);

                var result = new InboundChannel(name, readerSemaphore, exclusiveAccessSemaphore, file, capacity, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, writerSemaphore);

                return new OperationResult<InboundChannel>(OperationStatus.Completed, result);
            }
            catch
            {
                if (readerSemaphore != null)
                {
                    try { readerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(readerSemaphore);
                }

                file?.Dispose();

                LifecycleHelper.PlatformSpecificDispose(exclusiveAccessSemaphore);

                LifecycleHelper.PlatformSpecificDispose(hasMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(noMessagesEvent);
                
                LifecycleHelper.PlatformSpecificDispose(clientConnectedEvent);

                LifecycleHelper.PlatformSpecificDispose(writerSemaphore);

                throw;
            }
        }

        #endregion

        /// <summary>
        /// The flag indicating that there are active messages in the queue. This property is thread-safe.
        /// </summary>
        public bool HasMessages
        {
            get
            {
                return WaitWhenQueueHasMessages(TimeSpan.Zero) == OperationStatus.Completed;
            }
        }

        #region Read

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public OperationResult<ChannelState> Read(Action<MemoryMappedViewStream> readingDelegate)
        {
            return ReadWithTimeout(new ReadingOperationWithoutResult(readingDelegate), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public OperationResult<ChannelState> Read(Action<MemoryMappedViewStream> readingDelegate, TimeSpan timeout)
        {
            return ReadWithTimeout(new ReadingOperationWithoutResult(readingDelegate), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public OperationResult<ChannelState> Read(Action<MemoryMappedViewStream> readingDelegate, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return ReadWithCancellation(new ReadingOperationWithoutResult(readingDelegate), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, OperationStatus> readingDelegate)
        {
            return ReadWithTimeout(new ReadingOperationWithoutParameters(readingDelegate), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, OperationStatus> readingDelegate, TimeSpan timeout)
        {
            return ReadWithTimeout(new ReadingOperationWithoutParameters(readingDelegate), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, OperationStatus> readingDelegate, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return ReadWithCancellation(new ReadingOperationWithoutParameters(readingDelegate), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, object, OperationStatus> readingDelegate, object parameter)
        {
            return ReadWithTimeout(new ReadingOperationWithParameter(readingDelegate, parameter), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, object, OperationStatus> readingDelegate, object parameter, TimeSpan timeout)
        {
            return ReadWithTimeout(new ReadingOperationWithParameter(readingDelegate, parameter), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, or 
        /// any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, object, OperationStatus> readingDelegate, object parameter, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return ReadWithCancellation(new ReadingOperationWithParameter(readingDelegate, parameter), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, or 
        /// any OperationStatus returned by readingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Read(Func<MemoryMappedViewStream, object, CancellationToken, OperationStatus> readingDelegate, object parameter, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return ReadWithCancellation(new ReadingOperationWithCancellation(readingDelegate, parameter, cancellationToken), cancellationToken, timeout);
        }

        private OperationResult<ChannelState> ReadWithTimeout(ReadingOperation readingOperation, TimeSpan timeout)
        {
            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                gainedExclusiveAccess = _exclusiveAccessSemaphore.WaitOne(timeout);

                if (!gainedExclusiveAccess) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);
                
                return ReadAndSetEvents(readingOperation);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private OperationResult<ChannelState> ReadWithCancellation(ReadingOperation readingOperation, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return ReadWithTimeout(readingOperation, timeout);

            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _exclusiveAccessSemaphore }, timeout);

                if (index == 0) return new OperationResult<ChannelState>(OperationStatus.Cancelled, null);

                if (index == WaitHandle.WaitTimeout) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);

                gainedExclusiveAccess = true;

                return ReadAndSetEvents(readingOperation);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private OperationResult<ChannelState> ReadAndSetEvents(ReadingOperation readingOperation)
        {
            var result = ((ReaderMemoryManager)_memoryManager).Read(readingOperation);

            var channelState = result.Data;

            if (channelState != null && channelState.MessagesCount == 0)
            {
                _hasMessagesEvent.Reset();

                _noMessagesEvent.Set();
            }

            return result;
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task> readingDelegate)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithoutResult(readingDelegate), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task> readingDelegate, TimeSpan timeout)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithoutResult(readingDelegate), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, or OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task> readingDelegate, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await ReadWithCancellationAsync(new AsynchronousReadingOperationWithoutResult(readingDelegate), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> readingDelegate)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithoutParameters(readingDelegate), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> readingDelegate, TimeSpan timeout)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithoutParameters(readingDelegate), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, or 
        /// any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> readingDelegate, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await ReadWithCancellationAsync(new AsynchronousReadingOperationWithoutParameters(readingDelegate), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> readingDelegate, object parameter)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithParameter(readingDelegate, parameter), Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, 
        /// or any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> readingDelegate, object parameter, TimeSpan timeout)
        {
            return await ReadWithTimeoutAsync(new AsynchronousReadingOperationWithParameter(readingDelegate, parameter), timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, or 
        /// any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> readingDelegate, object parameter, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await ReadWithCancellationAsync(new AsynchronousReadingOperationWithParameter(readingDelegate, parameter), cancellationToken, timeout);
        }

        /// <summary>
        /// Reads and removes a message from the queue. This method is thread-safe.
        /// </summary>
        /// <param name="readingDelegate">Custom delegate to read using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.QueueIsEmpty, OperationStatus.Timeout, OperationStatus.Cancelled 
        /// (when cancellation token is cancelled or delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, or 
        /// any OperationStatus returned by readingDelegate.
        /// </returns>
        public async Task<OperationResult<ChannelState>> ReadAsync(Func<MemoryMappedViewStream, object, CancellationToken, Task<OperationStatus>> readingDelegate, object parameter, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await ReadWithCancellationAsync(new AsynchronousReadingOperationWithCancellation(readingDelegate, parameter, cancellationToken), cancellationToken, timeout);
        }

        private async Task<OperationResult<ChannelState>> ReadWithTimeoutAsync(AsynchronousReadingOperation readingOperation, TimeSpan timeout)
        {
            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;

                return await ReadAndSetEventsAsync(readingOperation);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private async Task<OperationResult<ChannelState>> ReadWithCancellationAsync(AsynchronousReadingOperation readingOperation, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await ReadWithTimeoutAsync(readingOperation, timeout);

            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, cancellationToken, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;

                return await ReadAndSetEventsAsync(readingOperation);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private async Task<OperationResult<ChannelState>> ReadAndSetEventsAsync(AsynchronousReadingOperation readingOperation)
        {
            var result = await ((ReaderMemoryManager)_memoryManager).ReadAsync(readingOperation);

            var channelState = result.Data;

            if (channelState != null && channelState.MessagesCount == 0)
            {
                _hasMessagesEvent.Reset();

                _noMessagesEvent.Set();
            }

            return result;
        }

        #endregion

        #region WaitWhenQueueHasMessages

        /// <summary>
        /// Synchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        public void WaitWhenQueueHasMessages()
        {
            WaitWhenQueueHasMessages(Timeout.InfiniteTimeSpan);
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
            ThrowIfDisposed();

            var completed = _hasMessagesEvent.WaitOne(timeout);

            return completed ? OperationStatus.Completed : OperationStatus.Timeout;
        }

        /// <summary>
        /// Synchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public OperationStatus WaitWhenQueueHasMessages(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return WaitWhenQueueHasMessages(timeout);

            ThrowIfDisposed();

            var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _hasMessagesEvent }, timeout);

            if (index == 0) return OperationStatus.Cancelled;

            if (index == WaitHandle.WaitTimeout) return OperationStatus.Timeout;

            return OperationStatus.Completed;
        }

        /// <summary>
        /// Asynchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        public async Task WhenQueueHasMessagesAsync()
        {
            await WhenQueueHasMessagesAsync(Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Asynchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public async Task<OperationStatus> WhenQueueHasMessagesAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();

            return await AsyncHelper.GetTaskForWaitHandle(_hasMessagesEvent, timeout);
        }

        /// <summary>
        /// Asynchronously waits until writer will add new messafe to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public async Task<OperationStatus> WhenQueueHasMessagesAsync(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await WhenQueueHasMessagesAsync(timeout);

            ThrowIfDisposed();

            return await AsyncHelper.GetTaskForWaitHandle(_hasMessagesEvent, cancellationToken, timeout);
        }

        #endregion

        #region IDisposable

        protected override void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException($"{nameof(InboundChannel)} {Name}");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try { _readerSemaphore?.Release(); } catch { }

                    base.Dispose(true);
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Object

        /// <summary>
        /// Compares this InboundChannel with another one.
        /// </summary>
        /// <param name="other">Another instance of InboundChannel.</param>
        /// <returns>True when not null and equals.</returns>
        public override bool Equals(object other)
        {
            ThrowIfDisposed();

            var ic = other as InboundChannel;

            if (ic == null) return false;

            return _memoryManager.Equals(ic._memoryManager);
        }

        /// <summary>
        /// Get hash for this InboundChannel.
        /// </summary>
        /// <returns>
        /// Int hash.
        /// </returns>
        public override int GetHashCode()
        {
            ThrowIfDisposed();

            return _memoryManager.GetHashCode();
        }

        #endregion
    }
}
