In this sample client UWP app sends file path string to Windows Service, and Windows Service sends back content of the file.

To run sample
0) Check that Package Family Name of UwpClient is 3702ca08-983b-4b98-8f6a-fc949a30e6f0_1tefvgkrx7v4m, if it is different, copy actual PFN, run SidTool and paste it there. It will generate SID, replace constant in the SampleService.cs with generated value.
1) Build solution in Debug mode
2) Run \WindowsService\Install.cmd as Administrator to install and start SampleService
3) Launch UwpClient and enter path to some file that usually cannot be accessed from UWP app, for example, C:\WINDOWS\System32\drivers\etc\hosts
4) You will see content of that file
5) Run \WindowsService\Uninstall.cmd as Administrator to remove SampleService

The code for this application and service is only intended to demonstrate the library's capabilities and should not be used in production!