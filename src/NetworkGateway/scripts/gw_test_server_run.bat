START CMD /C "kill_all.bat"

timeout 3

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

timeout 1

START CMD /C "scnetworkgateway.exe personal scnetworkgateway.xml .db.output"

timeout 1

START CMD /C "sccode.exe MYDB_SERVER --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb"