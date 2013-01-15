::gw_test_bs.bat NUM_WORKERS MODE NUM_CONNS NUM_ECHOES MAX_TIME_SEC STATS_NAME APPS_MODE

START CMD /C "kill_all.bat"

timeout 1

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

START CMD /C "timeout 1 && sccode.exe MYDB_SERVER --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --SchedulerCount=1 --ChunksNumber=131072 --UserArguments="DbNumber=0 PortNumber=81 TestType=%7""

scnetworkgatewayloopedtest.exe personal scnetworkgateway.xml .db.output %1 %2 %3 %4 %5 %6
