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
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

using Internal = CorpusCallosum;

namespace CorpusCallosum.WinRT
{
    #region Writing delegates

    /// <summary>
    /// Simple delegate writing new message.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    public delegate void SynchronousWritingDelegate(IRandomAccessStream stream);

    /// <summary>
    /// Delegate writing new message and returning OperationStatus to commit or rollback memory allocation.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to rollback changes, OperationStatus.Success or other value to commit changes. 
    /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace and OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace cannot be returned by writing delegate,
    /// they will be processed as OperationStatus.DelegateFailed.
    /// </returns>
    public delegate OperationStatus SynchronousWritingDelegateWithResult(IRandomAccessStream stream);

    /// <summary>
    /// Delegate writing new message, receiving parameter and returning OperationStatus to commit or rollback memory allocation.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    /// <param name="parameter">Parameter passed on operation execution.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to rollback changes, OperationStatus.Success or other value to commit changes. 
    /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace and OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace cannot be returned by writing delegate,
    /// they will be processed as OperationStatus.DelegateFailed.
    /// </returns>
    public delegate OperationStatus SynchronousWritingDelegateWithParameterAndResult(IRandomAccessStream stream, object parameter);

    /// <summary>
    /// Simple delegate writing new message.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    public delegate IAsyncAction AsynchronousWritingDelegate(IRandomAccessStream stream);

    /// <summary>
    /// Delegate writing new message and returning OperationStatus to commit or rollback memory allocation.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to rollback changes, OperationStatus.Success or other value to commit changes. 
    /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace and OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace cannot be returned by writing delegate,
    /// they will be processed as OperationStatus.DelegateFailed.
    /// </returns>
    public delegate IAsyncOperation<OperationStatus> AsynchronousWritingDelegateWithResult(IRandomAccessStream stream);

    /// <summary>
    /// Delegate writing new message, receiving parameter and returning OperationStatus to commit or rollback memory allocation.
    /// </summary>
    /// <param name="stream">Stream representing memory requested for new message.</param>
    /// <param name="parameter">Parameter passed on operation execution.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to rollback changes, OperationStatus.Success or other value to commit changes. 
    /// OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace and OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace cannot be returned by writing delegate,
    /// they will be processed as OperationStatus.DelegateFailed.
    /// </returns>
    public delegate IAsyncOperation<OperationStatus> AsynchronousWritingDelegateWithParameterAndResult(IRandomAccessStream stream, object parameter);

    #endregion

    #region Reading delegates

    /// <summary>
    /// Simple delegate reading a message.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    public delegate void SynchronousReadingDelegate(IRandomAccessStream stream);

    /// <summary>
    /// Delegate reading a message and returning OperationStatus to release or keep the memory.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to keep message in the queue, OperationStatus.Success or other value to remove it.
    /// </returns>
    public delegate OperationStatus SynchronousReadingDelegateWithResult(IRandomAccessStream stream);

    /// <summary>
    /// Delegate reading a message, receiving parameter and returning OperationStatus to release or keep the memory.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    /// <param name="parameter">Parameter passed on operation execution.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to keep message in the queue, OperationStatus.Success or other value to remove it.
    /// </returns>
    public delegate OperationStatus SynchronousReadingDelegateWithParameterAndResult(IRandomAccessStream stream, object parameter);

    /// <summary>
    /// Simple delegate reading a message.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    public delegate IAsyncAction AsynchronousReadingDelegate(IRandomAccessStream stream);

    /// <summary>
    /// Delegate reading a message and returning OperationStatus to release or keep the memory.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to keep message in the queue, OperationStatus.Success or other value to remove it.
    /// </returns>
    public delegate IAsyncOperation<OperationStatus> AsynchronousReadingDelegateWithResult(IRandomAccessStream stream);

    /// <summary>
    /// Delegate reading a message, receiving parameter and returning OperationStatus to release or keep the memory.
    /// </summary>
    /// <param name="stream">Stream representing memory with message data.</param>
    /// <param name="parameter">Parameter passed on operation execution.</param>
    /// <returns>
    /// OperationStatus.Cancelled or OperationStatus.DelegateFailed to keep message in the queue, OperationStatus.Success or other value to remove it.
    /// </returns>
    public delegate IAsyncOperation<OperationStatus> AsynchronousReadingDelegateWithParameterAndResult(IRandomAccessStream stream, object parameter);

    #endregion

    internal static class DelegateHelper
    {
        #region Writing

        public static Action<MemoryMappedViewStream> Wrap(SynchronousWritingDelegate externalDelegate)
        {
            return (MemoryMappedViewStream stream) => externalDelegate?.Invoke(stream.AsRandomAccessStream());
        }

        public static Func<MemoryMappedViewStream, Internal.OperationStatus> Wrap(SynchronousWritingDelegateWithResult externalDelegate)
        {
            return (MemoryMappedViewStream stream) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                return (Internal.OperationStatus)externalDelegate.Invoke(stream.AsRandomAccessStream());
            };
        }

        public static Func<MemoryMappedViewStream, object, Internal.OperationStatus> Wrap(SynchronousWritingDelegateWithParameterAndResult externalDelegate)
        {
            return (MemoryMappedViewStream stream, object parameter) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                return (Internal.OperationStatus)externalDelegate.Invoke(stream.AsRandomAccessStream(), parameter);
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousWritingDelegate externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate != null) await externalDelegate.Invoke(stream.AsRandomAccessStream()).AsTask(cancellationToken);

                return Internal.OperationStatus.Completed;
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousWritingDelegateWithResult externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                var result = await externalDelegate.Invoke(stream.AsRandomAccessStream()).AsTask(cancellationToken);

                return (Internal.OperationStatus)result;
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousWritingDelegateWithParameterAndResult externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                var result = await externalDelegate.Invoke(stream.AsRandomAccessStream(), parameter).AsTask(cancellationToken);

                return (Internal.OperationStatus)result;
            };
        }

        #endregion

        #region Reading

        public static Action<MemoryMappedViewStream> Wrap(SynchronousReadingDelegate externalDelegate)
        {
            return (MemoryMappedViewStream stream) => externalDelegate?.Invoke(stream.AsRandomAccessStream());
        }

        public static Func<MemoryMappedViewStream, Internal.OperationStatus> Wrap(SynchronousReadingDelegateWithResult externalDelegate)
        {
            return (MemoryMappedViewStream stream) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                return (Internal.OperationStatus)externalDelegate.Invoke(stream.AsRandomAccessStream());
            };
        }

        public static Func<MemoryMappedViewStream, object, Internal.OperationStatus> Wrap(SynchronousReadingDelegateWithParameterAndResult externalDelegate)
        {
            return (MemoryMappedViewStream stream, object parameter) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                return (Internal.OperationStatus)externalDelegate.Invoke(stream.AsRandomAccessStream(), parameter);
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousReadingDelegate externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate != null) await externalDelegate.Invoke(stream.AsRandomAccessStream()).AsTask(cancellationToken);

                return Internal.OperationStatus.Completed;
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousReadingDelegateWithResult externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                var result = await externalDelegate.Invoke(stream.AsRandomAccessStream()).AsTask(cancellationToken);

                return (Internal.OperationStatus)result;
            };
        }

        public static Func<MemoryMappedViewStream, object, CancellationToken, Task<Internal.OperationStatus>> Wrap(AsynchronousReadingDelegateWithParameterAndResult externalDelegate)
        {
            return async (MemoryMappedViewStream stream, object parameter, CancellationToken cancellationToken) =>
            {
                if (externalDelegate == null) return Internal.OperationStatus.Completed;

                var result = await externalDelegate.Invoke(stream.AsRandomAccessStream(), parameter).AsTask(cancellationToken);

                return (Internal.OperationStatus)result;
            };
        }

        #endregion
    }
}
