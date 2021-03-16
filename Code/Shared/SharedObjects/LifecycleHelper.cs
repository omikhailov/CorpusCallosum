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

#if !UWP
using System.Security.AccessControl;
using System.Security.Principal;
using Acl = System.Collections.Generic.IEnumerable<System.Security.Principal.IdentityReference>;
#endif

namespace CorpusCallosum.SharedObjects
{
    internal static class LifecycleHelper
    {
        public const string LocalVisibilityPrefix = "Local";

        public const string GlobalVisibilityPrefix = "Global";

        #region Create

        public static Semaphore CreateWriterSemaphore(string channelName, Acl acl)
        {
            var fullName = channelName + "_ws";

            return CreateSemaphore(fullName, acl);
        }

        public static OperationResult<Semaphore> CreateAndSetWriterSemaphore(string channelName, Acl acl)
        {
            var fullName = channelName + "_ws";

            return CreateAndSetSemaphore(fullName, acl);
        }

        public static OperationResult<MemoryMappedFile> CreateMemoryMappedFile(string channelName, long capacity, Acl acl)
        {
            OperationStatus CheckExistance(string fileName)
            {
                MemoryMappedFile file = null;

                try
                {
                    file = MemoryMappedFile.OpenExisting(fileName);

                    return OperationStatus.ObjectAlreadyInUse;
                }
                catch
                { 
                    return OperationStatus.Completed; 
                }
                finally
                {
                    file?.Dispose();
                }
            }

            MemoryMappedFile result = null;

            var fullName = channelName + "_mmf";

            try
            {

                var existanceStatus = CheckExistance(fullName);
#if !UWP
                var security = new MemoryMappedFileSecurity();

                if (acl != null)
                {
                    foreach (var identity in acl)
                    {
                        security.AddAccessRule(new AccessRule<MemoryMappedFileRights>(identity, MemoryMappedFileRights.ReadWrite, AccessControlType.Allow));
                    }
                }

                try
                {
                    result = MemoryMappedFile.CreateOrOpen(fullName, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, security, HandleInheritability.None);
                }
                catch (InvalidOperationException)
                {
                    return new OperationResult<MemoryMappedFile>(OperationStatus.ElevationRequired, null);
                }
                catch (ArgumentException)
                {
                    return new OperationResult<MemoryMappedFile>(OperationStatus.CapacityIsGreaterThanLogicalAddressSpace, null);
                }
#else
                try
                {
                    result = MemoryMappedFile.CreateOrOpen(fullName, capacity, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.DelayAllocatePages, HandleInheritability.None);
                }
                catch (InvalidOperationException)
                {
                    return new OperationResult<MemoryMappedFile>(OperationStatus.ElevationRequired, null);
                }
                catch (ArgumentException)
                {
                    return new OperationResult<MemoryMappedFile>(OperationStatus.CapacityIsGreaterThanLogicalAddressSpace, null);
                }
#endif
                return new OperationResult<MemoryMappedFile>(existanceStatus, result);
            }            
            catch
            {
                result?.Dispose();

                throw;
            }
        }

        public static Semaphore CreateExclusiveAccessSemaphore(string channelName, Acl acl)
        {
            return CreateSemaphore(channelName + "_eas", acl);
        }

        public static EventWaitHandle CreateHasMessagesEvent(string channelName, Acl acl)
        {
            return CreateEvent(channelName + "_hme", acl);
        }

        internal static EventWaitHandle CreateNoMessagesEvent(string channelName, Acl acl)
        {
            return CreateEvent(channelName + "_nme", acl, true);
        }

        public static EventWaitHandle CreateClientConnectedEvent(string channelName, Acl acl)
        {
            return CreateEvent(channelName + "_cce", acl);
        }

        public static OperationResult<Semaphore> CreateAndSetReaderSemaphore(string channelName, Acl acl)
        {
            var fullName = channelName + "_rs";

            return CreateAndSetSemaphore(fullName, acl);
        }

        public static Semaphore CreateReaderSemaphore(string channelName, Acl acl)
        {
            return CreateSemaphore(channelName + "_rs", acl);
        }

        private static Semaphore CreateSemaphore(string fullName, Acl acl)
        {
            Semaphore result = null;

            try
            {
#if !UWP
                var security = new SemaphoreSecurity();

                if (acl != null)
                {
                    foreach (var identity in acl)
                    {
                        security.AddAccessRule(new SemaphoreAccessRule(identity, SemaphoreRights.Modify | SemaphoreRights.Synchronize, AccessControlType.Allow));
                    }
                }

                result = new Semaphore(1, 1, fullName, out bool _, security);
#else
                result = new Semaphore(1, 1, fullName);
#endif
                return result;
            }
            catch
            {
                PlatformSpecificDispose(result);

                throw;
            }
        }

        private static OperationResult<Semaphore> CreateAndSetSemaphore(string fullName, Acl acl)
        {
            var result = CreateSemaphore(fullName, acl);

            var registered = result.WaitOne(TimeSpan.Zero);

            if (!registered)
            {
                PlatformSpecificDispose(result);

                return new OperationResult<Semaphore>(OperationStatus.ObjectAlreadyInUse, null);
            }

            return new OperationResult<Semaphore>(OperationStatus.Completed, result);
        }

        private static EventWaitHandle CreateEvent(string fullName, Acl acl, bool isSet = false)
        {
            EventWaitHandle result = null;

            try
            {
#if !UWP
                var security = new EventWaitHandleSecurity();

                if (acl != null)
                {
                    foreach (var identity in acl)
                    {
                        security.AddAccessRule(new EventWaitHandleAccessRule(identity, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize, AccessControlType.Allow));
                    }
                }

                result = new EventWaitHandle(isSet, EventResetMode.ManualReset, fullName, out bool _, security);
#else
                result = new EventWaitHandle(isSet, EventResetMode.ManualReset, fullName);
#endif
                return result;
            }
            catch
            {
                PlatformSpecificDispose(result);

                throw;
            }
        }

        #endregion

        #region Open

        public static OperationResult<Semaphore> OpenAndSetWriterSemaphore(string channelName)
        {
            var fullName = channelName + "_ws";

            var result = OpenSemaphore(fullName);

            if (result.Status != OperationStatus.Completed) return result;

            var semaphore = result.Data;

            try
            {
                var writerRegistered = semaphore.WaitOne(TimeSpan.Zero);

                if (!writerRegistered)
                {
                    PlatformSpecificDispose(semaphore);

                    return new OperationResult<Semaphore>(OperationStatus.ObjectAlreadyInUse, null);
                }

                return result;
            }
            catch
            {
                PlatformSpecificDispose(semaphore);

                throw;
            }
        }

        public static OperationStatus OpenCommonSharedObjects(string channelFullName, out MemoryMappedFile file, out Semaphore exclusiveAccessSemaphore, 
            out EventWaitHandle hasMessagesEvent, out EventWaitHandle noMessagesEvent, out EventWaitHandle clientConnectedEvent)
        {
            file = null;

            exclusiveAccessSemaphore = null;

            hasMessagesEvent = null;

            noMessagesEvent = null;

            clientConnectedEvent = null;

            var fileOperationResult = OpenMemoryMappedFile(channelFullName);

            if (fileOperationResult.Status != OperationStatus.Completed) return fileOperationResult.Status;

            file = fileOperationResult.Data;            

            var exclusiveAccessSemaphoreOperationResult = OpenExclusiveAccessSemaphore(channelFullName);

            if (exclusiveAccessSemaphoreOperationResult.Status != OperationStatus.Completed)
            {
                file.Dispose(); file = null;

                return exclusiveAccessSemaphoreOperationResult.Status;
            }
             
            exclusiveAccessSemaphore = exclusiveAccessSemaphoreOperationResult.Data;

            var hasMessagesEventOperationResult = OpenHasMessagesEvent(channelFullName);

            if (hasMessagesEventOperationResult.Status != OperationStatus.Completed)
            {
                file.Dispose(); file = null;

                PlatformSpecificDispose(exclusiveAccessSemaphore); exclusiveAccessSemaphore = null;

                return hasMessagesEventOperationResult.Status;
            }
             
            hasMessagesEvent = hasMessagesEventOperationResult.Data;

            var noMessagesEventOperationResult = OpenNoMessagesEvent(channelFullName);

            if (noMessagesEventOperationResult.Status != OperationStatus.Completed)
            {
                file.Dispose(); file = null;

                PlatformSpecificDispose(exclusiveAccessSemaphore); exclusiveAccessSemaphore = null;

                PlatformSpecificDispose(hasMessagesEvent); hasMessagesEvent = null;

                return noMessagesEventOperationResult.Status;
            }
            
            noMessagesEvent = noMessagesEventOperationResult.Data;

            var clientConnectedEventOperationResult = OpenClientConnectedEvent(channelFullName);

            if (clientConnectedEventOperationResult.Status != OperationStatus.Completed)
            {
                file.Dispose(); file = null;

                PlatformSpecificDispose(exclusiveAccessSemaphore); exclusiveAccessSemaphore = null;

                PlatformSpecificDispose(hasMessagesEvent); hasMessagesEvent = null;

                PlatformSpecificDispose(noMessagesEvent); noMessagesEvent = null;
                                
                return clientConnectedEventOperationResult.Status;
            }
             
            clientConnectedEvent = clientConnectedEventOperationResult.Data;

            return OperationStatus.Completed;
        }

        public static OperationResult<MemoryMappedFile> OpenMemoryMappedFile(string channelFullName)
        {
            MemoryMappedFile result = null;

            var fullName = channelFullName + "_mmf";

            try
            {
                result = MemoryMappedFile.OpenExisting(fullName, MemoryMappedFileRights.ReadWrite);
            }
            catch (FileNotFoundException)
            {
                return new OperationResult<MemoryMappedFile>(OperationStatus.ObjectDoesNotExist, null);
            }
            catch
            {
                result?.Dispose();

                throw;
            }

            return new OperationResult<MemoryMappedFile>(OperationStatus.Completed, result);
        }

        public static OperationResult<Semaphore> OpenExclusiveAccessSemaphore(string channelFullName)
        {
            var fullName = channelFullName + "_eas";

            return OpenSemaphore(fullName);
        }

        public static OperationResult<Semaphore> OpenAndSetReaderSemaphore(string channelFullName)
        {
            var fullName = channelFullName + "_rs";

            var result = OpenSemaphore(fullName);

            if (result.Status != OperationStatus.Completed) return result;

            var semaphore = result.Data;

            try
            {
                var readerRegistered = semaphore.WaitOne(TimeSpan.Zero);

                if (!readerRegistered)
                {
                    PlatformSpecificDispose(semaphore);

                    return new OperationResult<Semaphore>(OperationStatus.ObjectAlreadyInUse, null);
                }
            }
            catch (Exception)
            {
                PlatformSpecificDispose(semaphore);

                throw;
            }

            return result;
        }

        public static OperationResult<EventWaitHandle> OpenHasMessagesEvent(string channelFullName)
        {
            var fullName = channelFullName + "_hme";

            return OpenEvent(fullName);
        }

        public static OperationResult<EventWaitHandle> OpenNoMessagesEvent(string channelFullName)
        {
            var fullName = channelFullName + "_nme";

            return OpenEvent(fullName);
        }

        public static OperationResult<EventWaitHandle> OpenClientConnectedEvent(string channelFullName)
        {
            var fullName = channelFullName + "_cce";

            return OpenEvent(fullName);
        }

        private static OperationResult<Semaphore> OpenSemaphore(string fullName)
        {
            Semaphore result = null;

            try
            {
                if (!Semaphore.TryOpenExisting(fullName, out result))
                {
                    return new OperationResult<Semaphore>(OperationStatus.ObjectDoesNotExist, null);
                }
                else
                {
                    return new OperationResult<Semaphore>(OperationStatus.Completed, result);
                }
            }
            catch (UnauthorizedAccessException)
            {
                PlatformSpecificDispose(result);

                return new OperationResult<Semaphore>(OperationStatus.AccessDenied, null);
            }
            catch
            {
                PlatformSpecificDispose(result);

                throw;
            }
        }

        private static OperationResult<EventWaitHandle> OpenEvent(string fullName)
        {
            EventWaitHandle result = null;

            try
            {
                if (!EventWaitHandle.TryOpenExisting(fullName, out result))
                {
                    return new OperationResult<EventWaitHandle>(OperationStatus.ObjectDoesNotExist, null);
                }
            }
            catch (UnauthorizedAccessException)
            {
                PlatformSpecificDispose(result);

                return new OperationResult<EventWaitHandle>(OperationStatus.AccessDenied, null);
            }
            catch (Exception)
            {
                PlatformSpecificDispose(result);

                throw;
            }

            return new OperationResult<EventWaitHandle>(OperationStatus.Completed, result);
        }

        #endregion

        #region Dispose

        public static void PlatformSpecificDispose(WaitHandle wh)
        {
#if DOTNETSTANDARD_1_3
            wh?.Dispose();
#else
            wh?.Close();
#endif
        }

        #endregion
    }
}
