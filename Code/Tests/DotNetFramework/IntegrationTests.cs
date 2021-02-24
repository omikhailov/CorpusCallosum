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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CorpusCallosum;
using CorpusCallosum.SharedObjects.MemoryManagement;
using System.IO;

namespace Tests
{
    [TestClass]
    public class IntegrationTests
    {
        private string GetUniqueName()
        {
            return "channel_" + Guid.NewGuid().ToString("N");
        }

        private void InconclusiveWhenNotCompleted<T>(OperationResult<T> operationResult)
        {
            if (operationResult.Status != OperationStatus.Completed) Assert.Inconclusive(operationResult.Status.ToString());
        }

        [TestMethod]
        public void OutboundChannelCanBeCreatedAndDisposed()
        {
            OutboundChannel channel = null;
            
            try
            {
                var result = Channel.CreateOutboundLocal(GetUniqueName());

                channel = result.Data;

                Assert.AreEqual(OperationStatus.Completed, result.Status);
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public void InboundChannelCanBeCreatedAndDisposed()
        {
            InboundChannel channel = null;

            try
            {
                var result = Channel.CreateInboundLocal(GetUniqueName());

                channel = result.Data;

                Assert.AreEqual(OperationStatus.Completed, result.Status);
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public void OutboundChannelNameCanBeReused()
        {
            var name = GetUniqueName();

            OutboundChannel channel = null;

            try
            {
                var result = Channel.CreateOutboundLocal(name);

                channel = result.Data;

                InconclusiveWhenNotCompleted(result);

                channel.Dispose();

                result = Channel.CreateOutboundLocal(name);

                channel = result.Data;

                Assert.AreEqual(OperationStatus.Completed, result.Status);
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public void InboundChannelNameCanBeReused()
        {
            var name = GetUniqueName();

            InboundChannel channel = null;

            try
            {
                var result = Channel.CreateInboundLocal(name);

                channel = result.Data;

                InconclusiveWhenNotCompleted(result);

                channel.Dispose();

                result = Channel.CreateInboundLocal(name);

                channel = result.Data;

                Assert.AreEqual(OperationStatus.Completed, result.Status);
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public void OutboundChannelCanBeOpened()
        {
            var name = GetUniqueName();

            InboundChannel inboundChannel = null;

            OutboundChannel outboundChannel = null;

            try
            {
                var inboundChannelResult = Channel.CreateInboundLocal(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var outboundChannelResult = Channel.OpenOutboundLocalNoncontainerized(name);

                outboundChannel = outboundChannelResult.Data;

                Assert.AreEqual(OperationStatus.Completed, outboundChannelResult.Status);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void InboundChannelCanBeOpened()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                Assert.AreEqual(OperationStatus.Completed, inboundChannelResult.Status);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void OutboundChannelCanWriteSynchronously()
        {
            OutboundChannel channel = null;

            try
            {
                var channelResult = Channel.CreateOutboundLocal(GetUniqueName());

                channel = channelResult.Data;

                InconclusiveWhenNotCompleted(channelResult);

                var data = new byte[11];

                var writingResult = channel.Write((stream) =>
                {
                    stream.Write(data, 0, data.Length);

                    return OperationStatus.Completed;
                },
                data.Length);

                Assert.AreEqual(OperationStatus.Completed, writingResult.Status);

                Assert.AreEqual(1, writingResult.Data.MessagesCount, "Message counter should increase after writing");
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public async Task OutboundChannelCanWriteAsynchronously()
        {
            OutboundChannel channel = null;

            try
            {
                var channelResult = Channel.CreateOutboundLocal(GetUniqueName());

                channel = channelResult.Data;

                InconclusiveWhenNotCompleted(channelResult);

                var data = new byte[11];

                var writingResult = await channel.WriteAsync(async (stream) =>
                {
                    await stream.WriteAsync(data, 0, data.Length);

                    return OperationStatus.Completed;
                },
                data.Length);

                Assert.AreEqual(OperationStatus.Completed, writingResult.Status);

                Assert.AreEqual(1, writingResult.Data.MessagesCount, "Message counter should increase after writing");
            }
            finally
            {
                channel?.Dispose();
            }
        }

        [TestMethod]
        public void InboundChannelCanReadSynchronously()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var data = new byte[5] { 1, 2, 3, 4, 5 };

                var writingResult = outboundChannel.Write((stream) =>
                {
                    stream.Write(data, 0, data.Length);

                    return OperationStatus.Completed;
                },
                data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var buffer = new byte[data.Length];

                var readingResult = inboundChannel.Read((stream) =>
                {
                    stream.Read(buffer, 0, data.Length);

                    return OperationStatus.Completed;
                });

                CollectionAssert.AreEqual(data, buffer);

                Assert.AreEqual(0, readingResult.Data.MessagesCount, "Message counter should decrease after writing");
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public async Task InboundChannelCanReadAsynchronously()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var data = new byte[5] { 1, 2, 3, 4, 5 };

                var writingResult = outboundChannel.Write((stream) =>
                {
                    stream.Write(data, 0, data.Length);

                    return OperationStatus.Completed;
                },
                data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var buffer = new byte[data.Length];

                var readingResult = await inboundChannel.ReadAsync(async (stream) =>
                {
                    await stream.ReadAsync(buffer, 0, data.Length);

                    return OperationStatus.Completed;
                });

                CollectionAssert.AreEqual(data, buffer);

                Assert.AreEqual(0, readingResult.Data.MessagesCount, "Message counter should decrease after reading");
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void MemoryManagerReusesFreeSpaceOnExactMatch()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var data = new byte[5];

                // Write two nodes

                var writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(outboundChannelResult);

                writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var previousAllocatedSpace = writingResult.Data.AllocatedSpace;

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                // Remove node #1

                var readingResult = inboundChannel.Read((stream) => { });

                InconclusiveWhenNotCompleted(readingResult);

                // Write node #3 in place of #1

                writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var allocatedSpace = writingResult.Data.AllocatedSpace;

                Assert.AreEqual(previousAllocatedSpace, allocatedSpace);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void MemoryManagerReusesFreeSpaceWhenItCanSplitFreeNode()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            var sizeOfNode = Marshal.SizeOf<Node>();

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var data = new byte[sizeOfNode];

                // Write two nodes having data with length >= size of node's header

                var writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(outboundChannelResult);

                writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                var previousAllocatedSpace = writingResult.Data.AllocatedSpace;

                InconclusiveWhenNotCompleted(writingResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                // Remove one

                var readingResult = inboundChannel.Read((stream) => { });

                InconclusiveWhenNotCompleted(readingResult);

                // Write third empty node

                writingResult = outboundChannel.Write((stream) => { stream.Write(new byte[0], 0, 0); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                // MM should split free node into active node and new free list node

                var allocatedSpace = writingResult.Data.AllocatedSpace;

                Assert.AreEqual(previousAllocatedSpace, allocatedSpace);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void MemoryManagerReusesFreeSpaceWhenAllocationRequired()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            const int messageSize = 5;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var data = new byte[messageSize];

                // Write two nodes

                var writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(outboundChannelResult);

                writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                // Remove one node ...

                var readingResult = inboundChannel.Read((stream) => { });

                InconclusiveWhenNotCompleted(readingResult);

                // ... to write node #3 in place of first node

                writingResult = outboundChannel.Write((stream) => { stream.Write(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                // Remove node #2

                readingResult = inboundChannel.Read((stream) => { });

                InconclusiveWhenNotCompleted(readingResult);

                // To force MM write next large node #4 in place of #2

                var previousAllocatedSpace = readingResult.Data.AllocatedSpace;

                var largeData = new byte[messageSize + 1];

                writingResult = outboundChannel.Write((stream) => { stream.Write(largeData, 0, largeData.Length); }, largeData.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var allocatedSpace = writingResult.Data.AllocatedSpace;

                // It is one byte larger than #2, so MM should allocate one additional byte after free node

                Assert.AreEqual(previousAllocatedSpace + 1, allocatedSpace);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public async Task ServerCanAwaitForClient()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var task = outboundChannel.WhenClientConnectedAsync(TimeSpan.FromSeconds(1));

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);
                
                var status = await task;

                Assert.AreEqual(OperationStatus.Completed, status);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public async Task OutboundChannelCanAwaitWhenQueueIsEmpty()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var data = new byte[5];

                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var writingResult = await outboundChannel.WriteAsync(async (stream) => { await stream.WriteAsync(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var task = outboundChannel.WhenQueueIsEmptyAsync(TimeSpan.FromSeconds(1));

                var readingResult = await inboundChannel.ReadAsync(async (stream) => { await stream.ReadAsync(data, 0, data.Length); });

                InconclusiveWhenNotCompleted(readingResult);

                var status = await task;

                Assert.AreEqual(OperationStatus.Completed, status);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public async Task InboundChannelCanAwaitWhenQueueHasMessages()
        {
            var name = GetUniqueName();

            OutboundChannel outboundChannel = null;

            InboundChannel inboundChannel = null;

            try
            {
                var data = new byte[5];

                var outboundChannelResult = Channel.CreateOutboundLocal(name);

                outboundChannel = outboundChannelResult.Data;

                InconclusiveWhenNotCompleted(outboundChannelResult);

                var inboundChannelResult = Channel.OpenInboundLocalNoncontainerized(name);

                inboundChannel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var task = inboundChannel.WhenQueueHasMessagesAsync(TimeSpan.FromSeconds(1));

                var writingResult = await outboundChannel.WriteAsync(async (stream) => { await stream.WriteAsync(data, 0, data.Length); }, data.Length);

                InconclusiveWhenNotCompleted(writingResult);

                var status = await task;

                Assert.AreEqual(OperationStatus.Completed, status);
            }
            finally
            {
                inboundChannel?.Dispose();

                outboundChannel?.Dispose();
            }
        }

        [TestMethod]
        public void ChannelCanGetChannelState()
        {
            InboundChannel channel = null;

            try
            {
                var inboundChannelResult = Channel.CreateInboundLocal(GetUniqueName());

                channel = inboundChannelResult.Data;

                InconclusiveWhenNotCompleted(inboundChannelResult);

                var state = channel.GetChannelState();

                Assert.AreEqual(Marshal.SizeOf<Header>(), state.AllocatedSpace);

                Assert.AreEqual(0, state.MessagesCount);
            }
            finally
            {
                channel?.Dispose();
            }
        }
    }
}
