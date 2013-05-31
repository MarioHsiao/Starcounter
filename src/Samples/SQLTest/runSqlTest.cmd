pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
scweaver.exe s\SQLTest\SQLTest.exe
sccode.exe SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath=s\SQLTest\.starcounter\SQLTest.exe --FLAG:UseConsole --FLAG:NoNetworkGateway
popd
