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
using CorpusCallosum.SharedObjects.MemoryManagement.WritingOperations;

#if !UWP
using Acl = System.Collections.Generic.IEnumerable<System.Security.Principal.IdentityReference>;
#endif

namespace CorpusCallosum
{
    /// <summary>
    /// Outbound channel to write messages into the queue.
    /// </summary>
    public sealed class OutboundChannel : Channel, IDisposable
    {
        #region Instantiation

        internal OutboundChannel(string name, Semaphore writerSemaphore, MemoryMappedFile file, long capacity, 
            Semaphore exclusiveAccessSemaphore, EventWaitHandle hasMessagesEvent, EventWaitHandle noMessagesEvent, EventWaitHandle clientConnectedEvent, Semaphore readerSemaphore)
            : base(name, writerSemaphore, exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, readerSemaphore)
        {
            _isClient = false;            

            _memoryManager = new WriterMemoryManager(file, capacity);
        }

        internal OutboundChannel(string name, Semaphore writerSemaphore, MemoryMappedFile file,
            Semaphore exclusiveAccessSemaphore, EventWaitHandle hasMessagesEvent, EventWaitHandle noMessagesEvent, EventWaitHandle clientConnectedEvent, Semaphore readerSemaphore)
            : base(name, writerSemaphore, exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, readerSemaphore)
        {
            _isClient = true;            

            _memoryManager = new WriterMemoryManager(file);

            _clientConnectedEvent.Set();
        }

        internal static OperationResult<OutboundChannel> Create(string fullName, string name, long capacity, Acl acl)
        {
            Semaphore writerSemaphore = null;

            MemoryMappedFile file = null;

            Semaphore exclusiveAccessSemaphore = null;

            EventWaitHandle hasMessagesEvent = null;

            EventWaitHandle noMessagesEvent = null;

            EventWaitHandle clientConnectedEvent = null;

            Semaphore readerSemaphore = null;

            try
            {
                var writerSemaphoreOperationResult = LifecycleHelper.CreateAndSetWriterSemaphore(fullName, acl);

                if (writerSemaphoreOperationResult.Status != OperationStatus.Completed) return new OperationResult<OutboundChannel>(writerSemaphoreOperationResult.Status, null);
                
                writerSemaphore = writerSemaphoreOperationResult.Data;

                var fileOperationResult = LifecycleHelper.CreateMemoryMappedFile(fullName, capacity, acl);

                if (fileOperationResult.Status != OperationStatus.Completed)
                {
                    try { writerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(writerSemaphore);

                    return new OperationResult<OutboundChannel>(fileOperationResult.Status, null);
                }
                
                file = fileOperationResult.Data;

                exclusiveAccessSemaphore = LifecycleHelper.CreateExclusiveAccessSemaphore(fullName, acl);

                hasMessagesEvent = LifecycleHelper.CreateHasMessagesEvent(fullName, acl);

                noMessagesEvent = LifecycleHelper.CreateNoMessagesEvent(fullName, acl);

                clientConnectedEvent = LifecycleHelper.CreateClientConnectedEvent(fullName, acl);

                readerSemaphore = LifecycleHelper.CreateReaderSemaphore(fullName, acl);

                var result = new OutboundChannel(name, writerSemaphore, file, capacity,
                    exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, readerSemaphore);

                return new OperationResult<OutboundChannel>(OperationStatus.Completed, result);
            }
            catch
            {
                if (writerSemaphore != null)
                {
                    try { writerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(writerSemaphore);
                }

                file?.Dispose();

                LifecycleHelper.PlatformSpecificDispose(exclusiveAccessSemaphore);

                LifecycleHelper.PlatformSpecificDispose(hasMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(noMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(clientConnectedEvent);

                LifecycleHelper.PlatformSpecificDispose(readerSemaphore);

                throw;
            }
        }

        internal static OperationResult<OutboundChannel> Open(string fullName, string name)
        {
            Semaphore writerSemaphore = null;

            MemoryMappedFile file = null;

            Semaphore exclusiveAccessSemaphore = null;

            EventWaitHandle hasMessagesEvent = null;

            EventWaitHandle noMessagesEvent = null;

            EventWaitHandle clientConnectedEvent = null;

            try
            {
                var writerSemaphoreOperationResult = LifecycleHelper.OpenAndSetWriterSemaphore(fullName);

                if (writerSemaphoreOperationResult.Status != OperationStatus.Completed) return new OperationResult<OutboundChannel>(writerSemaphoreOperationResult.Status, null);
                
                writerSemaphore = writerSemaphoreOperationResult.Data;

                var openCommonSharedObjectsStatus = LifecycleHelper.OpenCommonSharedObjects(fullName, out file, out exclusiveAccessSemaphore, out hasMessagesEvent, out noMessagesEvent, out clientConnectedEvent);

                if (openCommonSharedObjectsStatus != OperationStatus.Completed)
                {
                    try { writerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(writerSemaphore);

                    return new OperationResult<OutboundChannel>(openCommonSharedObjectsStatus, null);
                }

                var result = new OutboundChannel(name, writerSemaphore, file, exclusiveAccessSemaphore, hasMessagesEvent, noMessagesEvent, clientConnectedEvent, null);

                return new OperationResult<OutboundChannel>(OperationStatus.Completed, result);
            }
            catch
            {
                if (writerSemaphore != null)
                {
                    try { writerSemaphore.Release(); } catch { }

                    LifecycleHelper.PlatformSpecificDispose(writerSemaphore);
                }

                file?.Dispose();

                LifecycleHelper.PlatformSpecificDispose(exclusiveAccessSemaphore);

                LifecycleHelper.PlatformSpecificDispose(hasMessagesEvent);

                LifecycleHelper.PlatformSpecificDispose(noMessagesEvent);
                
                LifecycleHelper.PlatformSpecificDispose(clientConnectedEvent);

                throw;
            }
        }

        #endregion

        /// <summary>
        /// The flag indicating that there are no active messages in the queue. This property is thread-safe.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return WaitWhenQueueIsEmpty(TimeSpan.Zero) == OperationStatus.Completed;
            }
        }

        #region Write

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace.
        /// </returns>
        public OperationResult<ChannelState> Write(Action<MemoryMappedViewStream> writingDelegate, long length)
        {
            return WriteWithTimeout(new WritingOperationWithoutResult(writingDelegate), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace.
        /// </returns>
        public OperationResult<ChannelState> Write(Action<MemoryMappedViewStream> writingDelegate, long length, TimeSpan timeout)
        {
            return WriteWithTimeout(new WritingOperationWithoutResult(writingDelegate), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace.
        /// </returns>
        public OperationResult<ChannelState> Write(Action<MemoryMappedViewStream> writingDelegate, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return WriteWithCancellation(new WritingOperationWithoutResult(writingDelegate), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, OperationStatus> writingDelegate, long length)
        {
            return WriteWithTimeout(new WritingOperationWithoutParameters(writingDelegate), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, OperationStatus> writingDelegate, long length, TimeSpan timeout)
        {
            return WriteWithTimeout(new WritingOperationWithoutParameters(writingDelegate), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, OperationStatus> writingDelegate, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return WriteWithCancellation(new WritingOperationWithoutParameters(writingDelegate), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, object, OperationStatus> writingDelegate, object parameter, long length)
        {
            return WriteWithTimeout(new WritingOperationWithParameter(writingDelegate, parameter), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, object, OperationStatus> writingDelegate, object parameter, long length, TimeSpan timeout)
        {
            return WriteWithTimeout(new WritingOperationWithParameter(writingDelegate, parameter), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, object, OperationStatus> writingDelegate, object parameter, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return WriteWithCancellation(new WritingOperationWithParameter(writingDelegate, parameter), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate.
        /// </returns>
        public OperationResult<ChannelState> Write(Func<MemoryMappedViewStream, object, CancellationToken, OperationStatus> writingDelegate, object parameter, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return WriteWithCancellation(new WritingOperationWithCancellation(writingDelegate, parameter, cancellationToken), length, cancellationToken, timeout);
        }

        private OperationResult<ChannelState> WriteWithTimeout(WritingOperation writingOperation, long length, TimeSpan timeout)
        {
            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                gainedExclusiveAccess = _exclusiveAccessSemaphore.WaitOne(timeout);

                if (!gainedExclusiveAccess) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);

                return WriteAndSetEvents(writingOperation, length);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private OperationResult<ChannelState> WriteWithCancellation(WritingOperation writingOperation, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return WriteWithTimeout(writingOperation, length, timeout);

            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _exclusiveAccessSemaphore }, timeout);

                if (index == 0) return new OperationResult<ChannelState>(OperationStatus.Cancelled, null);

                if (index == WaitHandle.WaitTimeout) return new OperationResult<ChannelState>(OperationStatus.Timeout, null);

                gainedExclusiveAccess = true;

                return WriteAndSetEvents(writingOperation, length);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private OperationResult<ChannelState> WriteAndSetEvents(WritingOperation writingOperation, long length)
        {
            var result = ((WriterMemoryManager)_memoryManager).Write(writingOperation, length);

            var channelState = result.Data;

            if (channelState != null && channelState.MessagesCount > 0)
            {
                _noMessagesEvent.Reset();

                _hasMessagesEvent.Set();
            }

            return result;
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task> writingDelegate, long length)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithoutResult(writingDelegate), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace        
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task> writingDelegate, long length, TimeSpan timeout)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithoutResult(writingDelegate), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace or OperationStatus.OutOfSpace
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task> writingDelegate, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await WriteWithCancellationAsync(new AsynchronousWritingOperationWithoutResult(writingDelegate), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> writingDelegate, long length)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithoutParameters(writingDelegate), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> writingDelegate, long length, TimeSpan timeout)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithoutParameters(writingDelegate), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, Task<OperationStatus>> writingDelegate, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await WriteWithCancellationAsync(new AsynchronousWritingOperationWithoutParameters(writingDelegate), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> writingDelegate, object parameter, long length)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithParameter(writingDelegate, parameter), length, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when delegate throws OperationCancelledException),
        /// OperationStatus.DelegateFailed (when delegate throws other exceptions), OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace,
        /// OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace, OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> writingDelegate, object parameter, long length, TimeSpan timeout)
        {
            return await WriteWithTimeoutAsync(new AsynchronousWritingOperationWithParameter(writingDelegate, parameter), length, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, object, Task<OperationStatus>> writingDelegate, object parameter, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await WriteWithCancellationAsync(new AsynchronousWritingOperationWithParameter(writingDelegate, parameter), length, cancellationToken, timeout);
        }

        /// <summary>
        /// Adds new message to the queue. This method is thread-safe.
        /// </summary>
        /// <param name="writingDelegate">Custom delegate to write using message's stream.</param>
        /// <param name="parameter">Delegate parameter object.</param>
        /// <param name="length">
        /// Specifies how much memory will be allocated for new message.
        /// Delegate will receive a stream with capacity limited by this parameter, but it is not forced to use all allocated space.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationResult with ChannelState and OperationStatus.Completed, OperationStatus.Timeout, OperationStatus.Cancelled (when cancellation token is cancelled or
        /// delegate throws OperationCancelledException), OperationStatus.DelegateFailed (when delegate throws other exceptions), 
        /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace, OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace,
        /// OperationStatus.OutOfSpace or any OperationStatus returned by writingDelegate
        /// </returns>
        public async Task<OperationResult<ChannelState>> WriteAsync(Func<MemoryMappedViewStream, object, CancellationToken, Task<OperationStatus>> writingDelegate, object parameter, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            return await WriteWithCancellationAsync(new AsynchronousWritingOperationWithCancellation(writingDelegate, parameter, cancellationToken), length, cancellationToken, timeout);
        }

        private async Task<OperationResult<ChannelState>> WriteWithTimeoutAsync(AsynchronousWritingOperation writingOperation, long length, TimeSpan timeout)
        {
            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;                

                return await WriteAndSetEventsAsync(writingOperation, length);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private async Task<OperationResult<ChannelState>> WriteWithCancellationAsync(AsynchronousWritingOperation writingOperation, long length, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await WriteWithTimeoutAsync(writingOperation, length, timeout);

            ThrowIfDisposed();

            bool gainedExclusiveAccess = false;

            try
            {
                var status = await AsyncHelper.GetTaskForWaitHandle(_exclusiveAccessSemaphore, cancellationToken, timeout);

                if (status != OperationStatus.Completed) return new OperationResult<ChannelState>(status, null);

                gainedExclusiveAccess = true;

                return await WriteAndSetEventsAsync(writingOperation, length);
            }
            finally
            {
                if (gainedExclusiveAccess) _exclusiveAccessSemaphore.Release();
            }
        }

        private async Task<OperationResult<ChannelState>> WriteAndSetEventsAsync(AsynchronousWritingOperation writingOperation, long length)
        {
            var result = await ((WriterMemoryManager)_memoryManager).WriteAsync(writingOperation, length);

            var channelState = result.Data;

            if (channelState != null && channelState.MessagesCount > 0)
            {
                _noMessagesEvent.Reset();

                _hasMessagesEvent.Set();
            }

            return result;
        }

        #endregion

        #region WaitWhenQueueIsEmpty

        /// <summary>
        /// Synchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        public void WaitWhenQueueIsEmpty()
        {
            WaitWhenQueueIsEmpty(Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Synchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public OperationStatus WaitWhenQueueIsEmpty(TimeSpan timeout)
        {
            ThrowIfDisposed();

            var completed = _noMessagesEvent.WaitOne(timeout);

            return completed ? OperationStatus.Completed : OperationStatus.Timeout;
        }

        /// <summary>
        /// Synchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public OperationStatus WaitWhenQueueIsEmpty(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return WaitWhenQueueIsEmpty(timeout);

            ThrowIfDisposed();

            var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, _noMessagesEvent }, timeout);

            if (index == 0) return OperationStatus.Cancelled;

            if (index == WaitHandle.WaitTimeout) return OperationStatus.Timeout;

            return OperationStatus.Completed;
        }

        /// <summary>
        /// Asynchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        public async Task WhenQueueIsEmptyAsync()
        {
            await WhenQueueIsEmptyAsync(Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Asynchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed or OperationStatus.Timeout.
        /// </returns>
        public async Task<OperationStatus> WhenQueueIsEmptyAsync(TimeSpan timeout)
        {
            ThrowIfDisposed();

            return await AsyncHelper.GetTaskForWaitHandle(_noMessagesEvent, timeout);
        }

        /// <summary>
        /// Asynchronously waits until reader will process all messages and queue will be empty. This method is thread-safe.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="timeout">Operation timeout.</param>
        /// <returns>
        /// OperationStatus.Completed, OperationStatus.Timeout or OperationStatus.Cancelled.
        /// </returns>
        public async Task<OperationStatus> WhenQueueIsEmptyAsync(CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (!cancellationToken.CanBeCanceled) return await WhenQueueIsEmptyAsync(timeout);

            ThrowIfDisposed();

            return await AsyncHelper.GetTaskForWaitHandle(_noMessagesEvent, cancellationToken, timeout);
        }

        #endregion

        #region IDisposable

        protected override void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException($"{nameof(OutboundChannel)} {Name}");
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    try { _writerSemaphore?.Release(); } catch { }

                    base.Dispose(true);
                }

                _isDisposed = true;
            }
        }

        #endregion

        #region Object

        /// <summary>
        /// Compares this OutboundChannel with another one.
        /// </summary>
        /// <param name="other">Another instance of OutboundChannel.</param>
        /// <returns>True when not null and equals.</returns>
        public override bool Equals(object other)
        {
            ThrowIfDisposed();

            var oc = other as OutboundChannel;

            if (oc == null) return false;

            return _memoryManager.Equals(oc._memoryManager);
        }

        /// <summary>
        /// Get hash for this OutboundChannel.
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
