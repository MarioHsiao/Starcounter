pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
scweaver.exe s\IndexQueryTest\IndexQueryTest.exe --FLAG:tocache
boot ACCOUNTTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --FLAG:UseConsole  --FLAG:NoNetworkGateway
popd
