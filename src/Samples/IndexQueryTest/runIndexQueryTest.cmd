pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
START ScConnMonitor PERSONAL .db.output
Weaver.exe s\IndexQueryTest\IndexQueryTest.exe --FLAG:tocache
boot ACCOUNTTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe
popd
