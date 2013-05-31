pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
scweaver.exe s\QueryProcessingTest\QueryProcessingTest.exe
sccode.exe QUERYPROCESSINGTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --FLAG:UseConsole  --FLAG:NoNetworkGateway
popd
