pushd C:\GitRepositories\Starcounter\Orange\bin\Debug
Weaver.exe s\IndexQueryTest\IndexQueryTest.exe --FLAG:tocache
scpmm ACCOUNTTEST AccountTest .db.output
START ScConnMonitor PERSONAL .db.output
boot ACCOUNTTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe
popd
