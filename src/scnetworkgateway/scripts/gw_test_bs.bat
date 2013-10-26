:: gw_test_bs.bat 1_NUM_GATEWAY_WORKERS "2_MODE 3_NUM_CONNS 4_NUM_ECHOES 5_MAX_TIME_SEC 6_STATS_NAME" 7_SCHED_NUM 8_APPS_MODE 9_APPS_PORT_NUM 10_NUM_CHUNKS 12_DB_OPTIONS
:: gw_test_bs.bat 1 "MODE_GATEWAY_SMC_RAW 1000 10000000 100 BSStatsBlaBla" 1 MODE_GATEWAY_SMC_RAW 81 131072 --FLAG:NoDb

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL

:: Sleeping some time using ping.
ping -n 10 127.0.0.1 > nul

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=MYDB

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% EXIT 1
IF NOT EXIST %DB_OUT_DIR% EXIT 1

:: Starting IPC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Starting database memory management process.
START CMD /C "scdata.exe %DB_NAME% %DB_NAME% %DB_OUT_DIR%"

:: Starting log writer process.
START CMD /C "scdblog.exe %DB_NAME% %DB_NAME% %DB_OUT_DIR%"

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"

:: Starting database with some delay.
START CMD /C "timeout 2 && sccode.exe %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath=s\NetworkIoTest\NetworkIoTest.exe %7 --SchedulerCount=%3 --ChunksNumber=%6 --GatewayWorkersNumber=%1 --UserArguments="DbNumber=0 PortNumber=%5 TestType=%4""

:: Starting network gateway.
scnetworkgatewayloopedtest.exe personal scnetworkgateway.xml %DB_OUT_DIR% %1 %~2
