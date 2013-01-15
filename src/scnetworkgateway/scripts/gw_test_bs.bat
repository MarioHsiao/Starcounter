:: gw_test_bs.bat 1_NUM_WORKERS 2_MODE 3_NUM_CONNS 4_NUM_ECHOES 5_MAX_TIME_SEC 6_STATS_NAME 7_APPS_MODE 8_SCHED_NUM
:: gw_test_bs.bat 1 MODE_GATEWAY_SMC_RAW 100 10000000 100 BSStatsBlaBla MODE_GATEWAY_SMC_RAW 1

START CMD /C "kill_all.bat"

timeout 1

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

START CMD /C "timeout 1 && sccode.exe MYDB_SERVER --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --SchedulerCount=%8 --ChunksNumber=131072 --UserArguments="DbNumber=0 PortNumber=81 TestType=%7""

scnetworkgatewayloopedtest.exe personal scnetworkgateway.xml .db.output %1 %2 %3 %4 %5 %6
