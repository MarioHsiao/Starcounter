pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
scweaver.exe s\QueryProcessingTest\QueryProcessingTest.exe
sccode.exe 1 QUERYPROCESSINGTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --FLAG:UseConsole  --FLAG:NoNetworkGateway
popd
