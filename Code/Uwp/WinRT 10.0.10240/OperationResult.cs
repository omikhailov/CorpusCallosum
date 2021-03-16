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

using Internal = CorpusCallosum;

namespace CorpusCallosum.WinRT
{
    /// <summary>
    /// Common interface for operation results.
    /// </summary>
    public interface IOperationResult
    {
        /// <summary>
        /// Status of the operation.
        /// </summary>
        OperationStatus Status { get; }
    }

    /// <summary>
    /// Result of the operation that changes channel state.
    /// </summary>
    public class ChannelStateOperationResult : IOperationResult
    {
        internal ChannelStateOperationResult(Internal.OperationResult<Internal.ChannelState> @internal)
        {
            Status = (OperationStatus)@internal.Status;

            Data = new ChannelState(@internal.Data);
        }

        /// <summary>
        /// Status of the operation.
        /// </summary>
        public OperationStatus Status { get; internal set; }

        /// <summary>
        /// The state of the channel afer operation.
        /// </summary>
        public ChannelState Data { get; internal set; }
    }

    /// <summary>
    /// Result of the operation that opens or creates new inbound channel.
    /// </summary>
    public class NewInboundChannelOperationResult : IOperationResult
    {
        internal NewInboundChannelOperationResult(Internal.OperationResult<Internal.InboundChannel> @internal)
        {
            Status = (OperationStatus)@internal.Status;

            Data = @internal.Data == null ? null : new InboundChannel(@internal.Data);
        }

        /// <summary>
        /// Status of the operation.
        /// </summary>
        public OperationStatus Status { get; internal set; }

        /// <summary>
        /// Opened or created InboundChannel.
        /// </summary>
        public InboundChannel Data { get; internal set; }
    }

    /// <summary>
    /// Result of the operation that opens or creates new outbound channel.
    /// </summary>
    public class NewOutboundChannelOperationResult : IOperationResult
    {
        internal NewOutboundChannelOperationResult(Internal.OperationResult<Internal.OutboundChannel> @internal)
        {
            Status = (OperationStatus)@internal.Status;

            Data = @internal.Data == null ? null : new OutboundChannel(@internal.Data);
        }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public OperationStatus Status { get; internal set; }

        /// <summary>
        /// Opened or created OutboundChannel.
        /// </summary>
        public OutboundChannel Data { get; internal set; }
    }
}
