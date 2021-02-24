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

namespace CorpusCallosum.SharedObjects
{
    internal class AsyncHelper
    {
#if !DOTNETSTANDARD_1_3
        public static async Task<OperationStatus> GetTaskForWaitHandle(WaitHandle waitHandle, TimeSpan timeout)
        {
            if (waitHandle.WaitOne(TimeSpan.Zero)) return OperationStatus.Completed;

            var taskCompletionSource = new TaskCompletionSource<OperationStatus>();

            var state = new State(taskCompletionSource);

            lock (state)
            {
                state.ThreadPoolRegistration = ThreadPool.RegisterWaitForSingleObject(waitHandle, ThreadPoolCallback, state, timeout, true);
            }

            return await taskCompletionSource.Task;
        }

        public static async Task<OperationStatus> GetTaskForWaitHandle(WaitHandle waitHandle, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (waitHandle.WaitOne(TimeSpan.Zero)) return OperationStatus.Completed;

            var taskCompletionSource = new TaskCompletionSource<OperationStatus>();

            var state = new State(taskCompletionSource);

            lock (state)
            {
                state.ThreadPoolRegistration = ThreadPool.RegisterWaitForSingleObject(waitHandle, ThreadPoolCallbackWithCancellation, state, timeout, true);

                state.CancellationTokenRegistration = cancellationToken.Register(CancellationCallback, state);
            }

            return await taskCompletionSource.Task;
        }

        private static void ThreadPoolCallback(object stateObject, bool timedOut)
        {
            var state = (State)stateObject;

            lock (state)
            {
                var taskCompletionSource = state.TaskCompletionSource;

                if (timedOut)
                {
                    taskCompletionSource.TrySetResult(OperationStatus.Timeout);
                }
                else
                {
                    taskCompletionSource.TrySetResult(OperationStatus.Completed);
                }

                state.ThreadPoolRegistration.Unregister(null);
            }
        }

        private static void ThreadPoolCallbackWithCancellation(object stateObject, bool timedOut)
        {
            var state = (State)stateObject;

            lock (state)
            {
                var taskCompletionSource = state.TaskCompletionSource;

                var runningFirst = false;

                if (timedOut)
                {
                    runningFirst = taskCompletionSource.TrySetResult(OperationStatus.Timeout);
                }
                else
                {
                    runningFirst = taskCompletionSource.TrySetResult(OperationStatus.Completed);
                }

                state.ThreadPoolRegistration.Unregister(null);

                if (runningFirst) state.CancellationTokenRegistration.Dispose();
            }
        }

        private static void CancellationCallback(object stateObject)
        {
            var state = (State)stateObject;

            lock (state)
            {
                var taskCompletionSource = state.TaskCompletionSource;

                var runningFirst = taskCompletionSource.TrySetResult(OperationStatus.Cancelled);

                if (runningFirst) state.ThreadPoolRegistration.Unregister(null);
            }
        }

        private class State
        {
            public State(TaskCompletionSource<OperationStatus> taskCompletionSource)
            {
                TaskCompletionSource = taskCompletionSource;
            }

            public TaskCompletionSource<OperationStatus> TaskCompletionSource { get; set; }

            public RegisteredWaitHandle ThreadPoolRegistration { get; set; }

            public CancellationTokenRegistration CancellationTokenRegistration { get; set; }
        }
#else
        public static async Task<OperationStatus> GetTaskForWaitHandle(WaitHandle waitHandle, TimeSpan timeout)
        {
            if (waitHandle.WaitOne(TimeSpan.Zero)) return OperationStatus.Completed;

            return await Task.Run(() => 
            {               
                var completed = waitHandle.WaitOne(timeout);

                return completed ? OperationStatus.Completed : OperationStatus.Timeout;
            });
        }

        public static async Task<OperationStatus> GetTaskForWaitHandle(WaitHandle waitHandle, CancellationToken cancellationToken, TimeSpan timeout)
        {
            if (waitHandle.WaitOne(TimeSpan.Zero)) return OperationStatus.Completed;

            return await Task.Run(() =>
            {
                var index = WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle, waitHandle }, timeout);

                if (index == 0) return OperationStatus.Cancelled;

                if (index == WaitHandle.WaitTimeout) return OperationStatus.Timeout;

                return OperationStatus.Completed;
            });
        }
#endif
    }
}
