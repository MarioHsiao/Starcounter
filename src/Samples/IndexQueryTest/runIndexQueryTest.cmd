pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
scweaver.exe s\IndexQueryTest\IndexQueryTest.exe
sccode.exe ACCOUNTTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --FLAG:UseConsole  --FLAG:NoNetworkGateway
popd
