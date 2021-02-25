# CorpusCallosum
Library for interprocess communication between noncontainerized .Net and containerized UWP apps and services.

The main scenario for using this library is the interaction between a Windows Service and a desktop UWP app, although it can also be used to interact with a WPF / WinForms application or even for communication between two non-UWP processes. The library does not impose restrictions on the UWP app, it can be sideloaded or installed from the store. Also, no special capabilities are required.

The class responsible for communication between processes is called *Channel*, it encapsulates a memory-mapped file and synchronization objects. Channels can be:
- *Inbound* for reading or *Outbound* for writing.
- *Global*, visible from all user sessions, or *Local*, visible only from current user session.
- *Noncontainerized*, created from a normal Win32 process, or *Containerized*, created from UWP app running in the app container.
Further, the process that creates the channel and configures access control rules will be called *Server* and another process opening the channel - *Client*.

UWP apps can create both inbound and outbound channels, but they are limited to creating local channels only. They can open both inbound and outbound, local and global channels when server explicitly whitelisted app container's SID.
.Net Framework applications can create and open channels of any kind. However, to create global channels, they must be executed under user account having special permission "Create global objects".

Thus, in order to have duplex communication between the service and the application, you must create inbound global channel and outbound global channel on the service side and open them as noncontainerized on the app side.

```csharp
// On Windows Service side:

try
{
    var acl = new[] { new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), new SecurityIdentifier(YourUwpAppContainerSid) };

    var inboundChannelOperationResult = Channel.CreateInboundGlobal("some_channel_name", acl);
  
    inboundChannel = inboundChannelOperationResult.Data;
  
    if (inboundChannelOperationResult.Status != OperationStatus.Completed) { ... }
  
    var outboundChannelOperationResult = Channel.CreateOutboundGlobal("another_channel_name", acl);
  
    outboundChannel = outboundChannelOperationResult.Data;
  
    if (outboundChannelOperationResult.Status != OperationStatus.Completed) { ... }
}
catch
{
    inboundChannel?.Dispose();
    
    outboundChannel?.Dispose();
}
```

```csharp
// On UWP app side:

try
{
    var outboundChannelOperationResult = Channel.OpenOutboundGlobalNoncontainerized("some_channel_name"); // It's outbound on this side
    
    outboundChannel = outboundChannelOperationResult.Data;
    
    if (outboundChannelOperationResult.Status != OperationStatus.Completed) { ... }
        
    var inboundChannelOperationResult = Channel.OpenInboundGlobalNoncontainerized("another_channel_name");
  
    inboundChannel = inboundChannelOperationResult.Data;
  
    if (inboundChannelOperationResult.Status != OperationStatus.Completed) { ... }    
}
catch
{
    outboundChannel?.Dispose(); // Important: always dispose channels as soon as they became unnecessary. Don't forget to handle UWP app suspension.

    inboundChannel?.Dispose();
}
```

The SID of UWP app can be found in the Partner Center for apps uploaded to the Microsoft Store or generated from Package Family Name using the tool included in this repository.
