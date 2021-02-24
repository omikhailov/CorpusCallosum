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
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using CorpusCallosum.SharedObjects;
using CorpusCallosum.SharedObjects.MemoryManagement;

namespace CorpusCallosum
{
    public abstract partial class Channel
    {
        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name, params IdentityReference[] acl)
        {
            return CreateOutboundLocal(name, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name, long capacity, params IdentityReference[] acl)
        {
            return CreateOutboundLocal(name, capacity, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name, IEnumerable<IdentityReference> acl)
        {
            return CreateOutboundLocal(name, DefaultCapacity, acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundLocal(string name, long capacity, IEnumerable<IdentityReference> acl)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be at least {Header.Size} bytes");

            return OutboundChannel.Create(LifecycleHelper.LocalVisibilityPrefix + "\\" + name, name, capacity, WhenEmptyUse(acl, WellKnownSidType.AuthenticatedUserSid));
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission)
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundGlobal(string name, params IdentityReference[] acl)
        {
            return CreateOutboundGlobal(name, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission), or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundGlobal(string name, long capacity, params IdentityReference[] acl)
        {
            return CreateOutboundGlobal(name, capacity, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer)
        /// or OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission)
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundGlobal(string name, IEnumerable<IdentityReference> acl)
        {
            return CreateOutboundGlobal(name, DefaultCapacity, acl);
        }

        /// <summary>
        /// Creates or reopens channel for writing. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with OutboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another writer),
        /// OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission), or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<OutboundChannel> CreateOutboundGlobal(string name, long capacity, IEnumerable<IdentityReference> acl)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be at least {Header.Size} bytes");

            return OutboundChannel.Create(LifecycleHelper.GlobalVisibilityPrefix + "\\" + name, name, capacity, WhenEmptyUse(acl, WellKnownSidType.AuthenticatedUserSid));
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader)
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name, params IdentityReference[] acl)
        {
            return CreateInboundLocal(name, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name, long capacity, params IdentityReference[] acl)
        {
            return CreateInboundLocal(name, capacity, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed or OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader)
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name, IEnumerable<IdentityReference> acl = null)
        {
            return CreateInboundLocal(name, DefaultCapacity, acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in the local user session.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader)
        /// or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundLocal(string name, long capacity, IEnumerable<IdentityReference> acl)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be at least {Header.Size} bytes");

            return InboundChannel.Create(LifecycleHelper.LocalVisibilityPrefix + "\\" +  name, name, capacity, WhenEmptyUse(acl, WellKnownSidType.AuthenticatedUserSid));
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader)
        /// or OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission)
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundGlobal(string name, params IdentityReference[] acl)
        {
            return CreateInboundGlobal(name, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader),
        /// OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission) or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundGlobal(string name, long capacity, params IdentityReference[] acl)
        {
            return CreateInboundGlobal(name, capacity, (IEnumerable<IdentityReference>)acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader) or
        /// OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission)
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundGlobal(string name, IEnumerable<IdentityReference> acl)
        {
            return CreateInboundGlobal(name, DefaultCapacity, acl);
        }

        /// <summary>
        /// Creates or reopens channel for reading. Channel will be visible from processes in all user sessions.
        /// </summary>
        /// <param name="name">Channel name.</param>
        /// <param name="capacity">Capacity of the cannel's queue in bytes.</param>
        /// <param name="acl">
        /// The list of security indentifiers for entities allowed to access this channel.
        /// Use new System.Security.Principal.SecurityIdentifier(yourUwpAppContainerSidString) to grant access to UWP app.
        /// Empty list is equivalent of WellKnownSidType.AuthenticatedUserSid.
        /// </param>
        /// <returns>
        /// OperationResult with InboundChannel and OperationStatus.Completed, OperationStatus.ObjectAlreadyInUse (when channel is already in use by another reader),
        /// OperationStatus.ElevationRequired (when current process does not have "Create global objects" permission) or OperationStatus.CapacityIsGreaterThanLogicalAddressSpace
        /// </returns>
        public static OperationResult<InboundChannel> CreateInboundGlobal(string name, long capacity, IEnumerable<IdentityReference> acl)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name.Length == 0) throw new ArgumentException("Channel name required to create shared memory channel");

            if (capacity < Header.Size) throw new ArgumentException($"Channel capacity must be at least {Header.Size} bytes");

            return InboundChannel.Create(LifecycleHelper.GlobalVisibilityPrefix + "\\" + name, name, capacity, WhenEmptyUse(acl, WellKnownSidType.AuthenticatedUserSid));
        }

        private static IEnumerable<IdentityReference> WhenEmptyUse(IEnumerable<IdentityReference> acl, WellKnownSidType type)
        {
            var result = new List<IdentityReference>(acl ?? Enumerable.Empty<IdentityReference>());

            if (result.Count == 0) result.Add(new SecurityIdentifier(type, null));

            return result;
        }
    }
}
