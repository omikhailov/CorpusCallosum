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
using System.Threading.Tasks;

namespace CorpusCallosum.SharedObjects.MemoryManagement.WritingOperations
{
    internal class AsynchronousWritingOperationWithoutResult : AsynchronousWritingOperation
    {
        Func<MemoryMappedViewStream, Task> _writingDelegate;

        public AsynchronousWritingOperationWithoutResult(Func<MemoryMappedViewStream, Task> writingDelegate)
        {
            _writingDelegate = writingDelegate;
        }

        public override async Task<OperationStatus> WriteAsync(MemoryMappedFile file, long offset, long length)
        {
            MemoryMappedViewStream stream = null;

            try
            {
                stream = file.CreateViewStream(offset, length);

                try
                {
                    await _writingDelegate(stream);

                    return OperationStatus.Completed;
                }
                catch (OperationCanceledException)
                {
                    return OperationStatus.Cancelled;
                }
                catch
                {
                    return OperationStatus.DelegateFailed;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace;
            }
            catch (IOException)
            {
                return OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace;
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}
