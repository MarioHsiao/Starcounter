:: gw_test_bs.bat 1_NUM_WORKERS 2_MODE 3_NUM_CONNS 4_NUM_ECHOES 5_MAX_TIME_SEC 6_STATS_NAME 7_SCHED_NUM 8_APPS_MODE 9_APPS_PORT_NUM 10_NUM_CHUNKS 11_DB_OPTIONS

:: gw_test_bs.bat "1 MODE_GATEWAY_SMC_RAW 1000 10000000 100 BSStatsBlaBla" 1 MODE_GATEWAY_SMC_RAW 81 131072 --FLAG:NoDb

:: gw_test_bs.bat "1 MODE_GATEWAY_HTTP 1000 10000000 100 GwSimpleHttpRPS1Worker1000Conn" 1 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "1 MODE_GATEWAY_HTTP 10000 10000000 100 GwSimpleHttpRPS1Worker10000Conn" 1 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "1 MODE_GATEWAY_HTTP 100000 10000000 100 GwSimpleHttpRPS1Worker10000Conn" 1 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_HTTP 1000 10000000 100 GwSimpleHttpRPS2Worker1000Conn" 2 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_HTTP 10000 10000000 100 GwSimpleHttpRPS2Worker10000Conn" 2 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_HTTP 100000 10000000 100 GwSimpleHttpRPS2Worker100000Conn" 2 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_HTTP 1002 10000000 100 GwSimpleHttpRPS3Worker1000Conn" 3 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_HTTP 10002 10000000 100 GwSimpleHttpRPS3Worker10000Conn" 3 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_HTTP 100002 10000000 100 GwSimpleHttpRPS3Worker100000Conn" 3 MODE_GATEWAY_HTTP 81 131072 --FLAG:NoDb

:: gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 1000 10000000 100 GwSmcSimpleHttpRPS1Worker1000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 10000 10000000 100 GwSmcSimpleHttpRPS1Worker10000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 100000 10000000 100 GwSmcSimpleHttpRPS1Worker100000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 1000 10000000 100 GwSmcSimpleHttpRPS2Worker1000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 10000 10000000 100 GwSmcSimpleHttpRPS2Worker10000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 100000 10000000 100 GwSmcSimpleHttpRPS2Worker100000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 1002 10000000 100 GwSmcSimpleHttpRPS3Worker1000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 10002 10000000 100 GwSmcSimpleHttpRPS3Worker10000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb
:: gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 100002 10000000 100 GwSmcSimpleHttpRPS3Worker100000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 131072 --FLAG:NoDb

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL

:: Checking if everything is pre-created.
IF NOT EXIST .db ( MKDIR .db )
IF NOT EXIST .db.output ( MKDIR .db.output )
CMD /C "timeout 1"

:: Starting SMC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL .db.output"

:: Starting database with some delay.
START CMD /C "timeout 2 && sccode.exe MYDB_SERVER --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=NetworkIoTest\NetworkIoTest.exe %6 --SchedulerCount=%2 --ChunksNumber=%5 --UserArguments="DbNumber=0 PortNumber=%4 TestType=%3""

:: Starting network gateway.
scnetworkgatewayloopedtest.exe personal scnetworkgateway.xml .db.output "%1"
