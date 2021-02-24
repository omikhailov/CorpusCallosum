using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CorpusCallosum;

namespace WindowsService
{
    public partial class SampleService : ServiceBase
    {
        // Run SidTool to generate SID from Package Family Name

        const string AppContainerSid = "S-1-15-2-1924809120-3459309503-4167135798-1520870182-386175638-3792578823-1722515537";

        private EventLog _eventLog;

        private CancellationTokenSource _cts;

        private Task _listenerThread;

        public SampleService()
        {
            InitializeComponent();

            _eventLog = new EventLog();

            if (!EventLog.SourceExists("SampleService")) EventLog.CreateEventSource("SampleService", "SampleService");

            _eventLog.Source = "SampleService";

            _eventLog.Log = "SampleService";
        }

        protected override void OnStart(string[] args)
        {
            _cts = new CancellationTokenSource();

            _listenerThread = Task.Run(ListenAndSendFiles, _cts.Token);
        }

        protected override void OnStop()
        {
            _cts.Cancel();

            _cts.Dispose();
        }

        private async Task ListenAndSendFiles()
        {
            const long megabyte = 1024 * 1024;

            var cancellationToken = _cts.Token;

            OutboundChannel fileChannel = null;

            InboundChannel pathChannel = null;

            try
            {
                var acl = new[] { new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), new SecurityIdentifier(AppContainerSid) };

                var pathChannelOperationResult = Channel.CreateInboundGlobal("sample_path", megabyte, acl);

                var fileChannelOperationResult = Channel.CreateOutboundGlobal("sample_file", megabyte, acl);

                pathChannel = pathChannelOperationResult.Data;

                fileChannel = fileChannelOperationResult.Data;

                if (pathChannelOperationResult.Status == OperationStatus.ObjectAlreadyInUse || fileChannelOperationResult.Status == OperationStatus.ObjectAlreadyInUse)
                {
                    _eventLog.WriteEntry("Channel wasn't disposed during previous run of the service.");

                    return;
                }

                if (pathChannelOperationResult.Status == OperationStatus.ElevationRequired || fileChannelOperationResult.Status == OperationStatus.ElevationRequired)
                {
                    _eventLog.WriteEntry("Process doesn't have permission to create global shared objects. See https://docs.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/create-global-objects for details.");

                    return;
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    var waitingForPathStatus = await pathChannel.WhenQueueHasMessagesAsync(cancellationToken, Timeout.InfiniteTimeSpan);

                    if (waitingForPathStatus == OperationStatus.Cancelled) return;

                    string path = null;

                    var pathOperationResult = await pathChannel.ReadAsync(async (stream) =>
                    {
                        using (var reader = new StreamReader(stream)) path = await reader.ReadLineAsync();

                        return OperationStatus.Completed;
                    },
                    cancellationToken, Timeout.InfiniteTimeSpan);

                    if (pathOperationResult.Status == OperationStatus.Cancelled) return;

                    // Actually app is already connected, line below is just example of waiting for connection

                    var waitingForClientStatus = await fileChannel.WhenClientConnectedAsync(cancellationToken, Timeout.InfiniteTimeSpan);

                    if (waitingForClientStatus == OperationStatus.Cancelled) return;

                    FileStream fileStream = null;

                    try
                    {
                        var fileOpened = false;

                        try
                        {
                            fileStream = File.OpenRead(path);

                            fileOpened = true;
                        }
                        catch (Exception e)
                        {
                            _eventLog.WriteEntry(e.ToString());

                            await TryWriteString(fileChannel, "Cannot open this file. Please check Event Log for details.");
                        }

                        if (fileOpened)
                        {
                            var writingOperationResult = await fileChannel.WriteAsync(async (targetStream, parameter, token) =>
                            {
                                try
                                {
                                    var sourceStream = (Stream)parameter;

                                    await sourceStream.CopyToAsync(targetStream, 4096, token);

                                    return OperationStatus.Completed;
                                }
                                catch (Exception e)
                                {
                                    if (!(e is OperationCanceledException)) _eventLog.WriteEntry(e.ToString()); // WriteAsync does not throw, it returns OperationStatus.DelegateFailed

                                    throw;
                                }
                            },
                            fileStream, fileStream.Length, cancellationToken, Timeout.InfiniteTimeSpan);

                            if (writingOperationResult.Status == OperationStatus.Cancelled) return;

                            if (writingOperationResult.Status == OperationStatus.DelegateFailed) await TryWriteString(fileChannel, "Cannot send file. Please check Event Log for details.");

                            if (writingOperationResult.Status == OperationStatus.RequestedLengthIsGreaterThanLogicalAddressSpace || 
                                writingOperationResult.Status == OperationStatus.RequestedLengthIsGreaterThanVirtualAddressSpace ||
                                writingOperationResult.Status == OperationStatus.OutOfSpace)
                            {
                                await TryWriteString(fileChannel, "File is too large.");

                                _eventLog.WriteEntry($"Failed to send file {path} because it is too large.");
                            }
                        }
                    }
                    finally
                    {
                        fileStream?.Close();
                    }
                }
            }
            catch (Exception e)
            {
                _eventLog.WriteEntry(e.ToString());

                Stop();
            }
            finally
            {
                pathChannel?.Dispose();

                fileChannel?.Dispose();
            }
        }

        private static async Task TryWriteString(OutboundChannel channel, string message)
        {
            using (var messageStream = new MemoryStream())
            {
                using (var writer = new StreamWriter(messageStream))
                {
                    await writer.WriteAsync(message);

                    await writer.FlushAsync();

                    messageStream.Position = 0;

                    await channel.WriteAsync(async (stream) =>
                    {
                        await messageStream.CopyToAsync(stream);
                    },
                    messageStream.Length);
                }
            }
        }
    }
}
