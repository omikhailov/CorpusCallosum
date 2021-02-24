set scriptpath=%~dp0
set exepath=bin\Debug\WindowsService.exe
set fullpath=%scriptpath%%exepath%
sc create SampleService binPath=%fullpath%
sc start SampleService
pause