using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CorpusCallosum;

namespace UwpClient.ViewModels
{
    public class MainViewModel : IDisposable, INotifyPropertyChanged
    {
        private Task _initializationTask;

        private OutboundChannel _pathChannel;

        private InboundChannel _fileChannel;

        private MainViewModel()
        {
            _initializationTask = Initialize();
        }

        private static MainViewModel _instance;

        public static MainViewModel Instance
        {
            get
            {
                if (_instance == null) _instance = new MainViewModel();

                return _instance;
            }
        }

        private bool _areChannelsOpened;

        public bool AreChannelsOpened
        {
            get
            {
                return _areChannelsOpened;
            }
            set
            {
                _areChannelsOpened = value;

                OnPropertyChanged();
            }
        }

        private string _fileContent;

        public string FileContent
        {
            get
            {
                return _fileContent;
            }
            set
            {
                _fileContent = value;

                OnPropertyChanged();
            }
        }

        private string _filePath;

        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                _filePath = value;

                OnPropertyChanged();
            }
        }

        private bool _isProcessingButtonClick;

        public async Task CallWindowsService()
        {
            if (_isProcessingButtonClick) return;

            _isProcessingButtonClick = true;

            try
            {
                if (string.IsNullOrWhiteSpace(_filePath))
                {
                    FileContent = "Path cannot be empty!";

                    return;
                }

                FileContent = "Sending request to Windows Service...";

                using (var stringStream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(stringStream))
                    {
                        writer.WriteLine(_filePath);

                        await writer.FlushAsync();

                        stringStream.Position = 0;

                        var pathOperationResult = await _pathChannel.WriteAsync(async (stream) =>
                        {
                            await stringStream.CopyToAsync(stream);
                        },
                        stringStream.Length);
                    }
                }

                FileContent = "Waiting for response from Windows Service...";

                await _fileChannel.WhenQueueHasMessagesAsync();

                var fileOperationResult = await _fileChannel.ReadAsync(async (stream) =>
                {
                    try
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            FileContent = await reader.ReadToEndAsync(); // Delegate executes in the same thread, it's ok to don't use Dispatcher
                        }
                    }
                    catch (Exception e)
                    {
                        FileContent = e.ToString();
                    }
                });
            }
            catch (Exception e)
            {
                FileContent = e.ToString();
            }
            finally
            {
                _isProcessingButtonClick = false;
            }
        }

        private async Task Initialize()
        {
            const string ChannelIsBroken = "Last time when app connected to the service, channel wasn't disposed, now it is broken";

            const string AccessDenied = "Access to the channel denied";

            FileContent = "Connecting to Windows Service...";
            
            do
            {
                try
                {
                    if (_pathChannel == null)
                    {
                        var pathChannelOperationResult = Channel.OpenOutboundGlobalNoncontainerized("sample_path");

                        if (pathChannelOperationResult.Status == OperationStatus.ObjectAlreadyInUse)
                        {
                            FileContent = ChannelIsBroken;

                            return;
                        }

                        if (pathChannelOperationResult.Status == OperationStatus.AccessDenied)
                        {
                            FileContent = AccessDenied;

                            return;
                        }

                        if (pathChannelOperationResult.Status == OperationStatus.ObjectDoesNotExist)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                        }
                        else
                        {
                            _pathChannel = pathChannelOperationResult.Data;
                        }
                    }

                    if (_fileChannel == null)
                    {
                        var fileChannelOperationResult = Channel.OpenInboundGlobalNoncontainerized("sample_file");

                        if (fileChannelOperationResult.Status == OperationStatus.ObjectAlreadyInUse)
                        {
                            FileContent = ChannelIsBroken;

                            return;
                        }

                        if (fileChannelOperationResult.Status == OperationStatus.AccessDenied)
                        {
                            FileContent = AccessDenied;

                            return;
                        }

                        if (fileChannelOperationResult.Status == OperationStatus.ObjectDoesNotExist)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                        }
                        else
                        {
                            _fileChannel = fileChannelOperationResult.Data;
                        }
                    }
                }
                catch (Exception e)
                {
                    FileContent += "\n\n" + e.ToString();

                    return;
                }
            }
            while (_pathChannel == null || _fileChannel == null);

            FileContent = string.Empty;

            AreChannelsOpened = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static void Close()
        {
            if (_instance != null)
            {
                _instance.Dispose();

                _instance = null;
            }
        }

        public void Dispose()
        {
            _pathChannel?.Dispose();

            _fileChannel?.Dispose();
        }
    }
}
