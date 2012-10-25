pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
Weaver.exe s\SQLTest\SQLTest.exe --FLAG:tocache
boot SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=s\SQLTest\.starcounter\SQLTest.exe --FLAG:UseConsole --FLAG:NoNetworkGateway
popd
