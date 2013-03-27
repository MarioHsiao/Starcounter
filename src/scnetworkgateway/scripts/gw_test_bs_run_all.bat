:: gw_test_bs.bat 1_NUM_WORKERS 2_MODE 3_NUM_CONNS 4_NUM_ECHOES 5_MAX_TIME_SEC 6_STATS_NAME 7_SCHED_NUM 8_APPS_MODE 9_APPS_PORT_NUM 10_NUM_CHUNKS 11_DB_OPTIONS
:: gw_test_bs.bat "1 MODE_GATEWAY_SMC_RAW 1000 10000000 100 BSStatsBlaBla" 1 MODE_GATEWAY_SMC_RAW 81 131072

IF "%SC_RUN_GATEWAY_TESTS%"=="False" GOTO :EOF

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=MYDB

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% ( MKDIR %DB_DIR% )
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Building special version of Level1 solution.
IF NOT "%SC_RUN_TESTS_LOCALLY%"=="True" (
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" "..\..\src\Level1.sln" /p:Configuration=BuildServerTest;Platform=x64 /maxcpucount
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "timeout 2" 2>NUL
)

:: Creating database.
sccreatedb.exe -ip %DB_DIR% -lp %DB_DIR% %DB_NAME%

:: HTTP Tests

CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_HTTP 1000 10000000 100 GwSimpleHttpRPS1Worker1000Conn" 1 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_HTTP 10000 2000000 100 GwSimpleHttpRPS1Worker10000Conn" 1 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_HTTP 100000 2000000 100 GwSimpleHttpRPS1Worker100000Conn" 1 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_HTTP 1000 10000000 100 GwSimpleHttpRPS2Worker1000Conn" 2 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_HTTP 10000 2000000 100 GwSimpleHttpRPS2Worker10000Conn" 2 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_HTTP 100000 2000000 100 GwSimpleHttpRPS2Worker100000Conn" 2 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_HTTP 1002 10000000 100 GwSimpleHttpRPS3Worker1000Conn" 3 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_HTTP 10002 2000000 100 GwSimpleHttpRPS3Worker10000Conn" 3 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_HTTP 100002 2000000 100 GwSimpleHttpRPS3Worker100000Conn" 3 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 1000 10000000 100 GwSmcSimpleHttpRPS1Worker1000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 10000 2000000 100 GwSmcSimpleHttpRPS1Worker10000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_HTTP 100000 2000000 100 GwSmcSimpleHttpRPS1Worker100000Conn" 1 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 1000 10000000 100 GwSmcSimpleHttpRPS2Worker1000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 10000 2000000 100 GwSmcSimpleHttpRPS2Worker10000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_HTTP 100000 2000000 100 GwSmcSimpleHttpRPS2Worker100000Conn" 2 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 1002 10000000 100 GwSmcSimpleHttpRPS3Worker1000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 10002 2000000 100 GwSmcSimpleHttpRPS3Worker10000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_HTTP 100002 2000000 100 GwSmcSimpleHttpRPS3Worker100000Conn" 3 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Raw Port Tests

CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_RAW 1000 10000000 100 GwSimpleRawEchoes1Worker1000Conn" 1 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_RAW 10000 2000000 100 GwSimpleRawEchoes1Worker10000Conn" 1 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_RAW 100000 2000000 100 GwSimpleRawEchoes1Worker100000Conn" 1 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_RAW 1000 20000000 100 GwSimpleRawEchoes2Worker1000Conn" 2 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_RAW 10000 2000000 100 GwSimpleRawEchoes2Worker10000Conn" 2 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_RAW 100000 2000000 100 GwSimpleRawEchoes2Worker100000Conn" 2 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_RAW 1002 30000000 100 GwSimpleRawEchoes3Worker1000Conn" 3 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_RAW 10002 2000000 100 GwSimpleRawEchoes3Worker10000Conn" 3 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_RAW 100002 2000000 100 GwSimpleRawEchoes3Worker100000Conn" 3 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_RAW 1000 10000000 100 GwSmcSimpleRawEchoes1Worker1000Conn" 1 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_RAW 10000 2000000 100 GwSmcSimpleRawEchoes1Worker10000Conn" 1 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "1 MODE_GATEWAY_SMC_RAW 100000 2000000 100 GwSmcSimpleRawEchoes1Worker100000Conn" 1 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_RAW 1000 20000000 100 GwSmcSimpleRawEchoes2Worker1000Conn" 2 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_RAW 10000 2000000 100 GwSmcSimpleRawEchoes2Worker10000Conn" 2 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "2 MODE_GATEWAY_SMC_RAW 100000 2000000 100 GwSmcSimpleRawEchoes2Worker100000Conn" 2 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_RAW 1002 30000000 100 GwSmcSimpleRawEchoes3Worker1000Conn" 3 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_RAW 10002 2000000 100 GwSmcSimpleRawEchoes3Worker10000Conn" 3 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat "3 MODE_GATEWAY_SMC_RAW 100002 2000000 100 GwSmcSimpleRawEchoes3Worker100000Conn" 3 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO All gateway performance tests finished successfully!

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the performance test! 1>&2
CMD /C "kill_all.bat" 2>NUL
EXIT 1