IF "%SC_RUN_GATEWAY_TESTS%"=="False" GOTO :EOF

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=MYDB

staradmin kill all

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% ( MKDIR %DB_DIR% )
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Creating database.
sccreatedb.exe -tmpm 1 -ip %DB_DIR% %DB_NAME%

:: HTTP Tests

CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_HTTP 1000 10000000 100 gateway_simple_http_rps_1worker_1000conns" 1 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_HTTP 10000 10000000 100 gateway_simple_http_rps_1worker_10000conns" 1 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_HTTP 100000 5000000 100 gateway_simple_http_rps_1worker_100000conns" 1 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_HTTP 1000 10000000 100 gateway_simple_http_rps_2workers_1000conns" 2 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_HTTP 10000 10000000 100 gateway_simple_http_rps_2workers_10000conns" 2 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_HTTP 100000 5000000 100 gateway_simple_http_rps_2workers_100000conns" 2 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_HTTP 1002 10000000 100 gateway_simple_http_rps_3workers_1000conns" 3 MODE_GATEWAY_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_HTTP 10002 10000000 100 gateway_simple_http_rps_3workers_10000conns" 3 MODE_GATEWAY_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_HTTP 100002 5000000 100 gateway_simple_http_rps_3workers_100000conns" 3 MODE_GATEWAY_HTTP 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_HTTP 1000 10000000 100 gateway_with_smc_simple_http_rps_1worker_1000conns" 1 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_HTTP 10000 5000000 100 gateway_with_smc_simple_http_rps_1worker_10000conns" 1 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_HTTP 100000 5000000 100 gateway_with_smc_simple_http_rps_1worker_100000conns" 1 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_HTTP 1000 10000000 100 gateway_with_smc_simple_http_rps_2workers_1000conns" 2 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_HTTP 10000 10000000 100 gateway_with_smc_simple_http_rps_2workers_10000conns" 2 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_HTTP 100000 5000000 100 gateway_with_smc_simple_http_rps_2workers_100000conns" 2 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_HTTP 1002 10000000 100 gateway_with_smc_simple_http_rps_3workers_1000conns" 3 MODE_GATEWAY_SMC_HTTP 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_HTTP 10002 10000000 100 gateway_with_smc_simple_http_rps_3workers_10000conns" 3 MODE_GATEWAY_SMC_HTTP 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_HTTP 100002 5000000 100 gateway_with_smc_simple_http_rps_3workers_100000conns" 3 MODE_GATEWAY_SMC_HTTP 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Raw Port Tests

CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_RAW 1000 30000000 100 gateway_simple_raw_eps_1worker_1000conns" 1 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_RAW 10000 30000000 100 gateway_simple_raw_eps_1worker_10000conns" 1 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_RAW 100000 10000000 100 gateway_simple_raw_eps_1worker_100000conns" 1 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_RAW 1000 30000000 100 gateway_simple_raw_eps_2workers_1000conns" 2 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_RAW 10000 30000000 100 gateway_simple_raw_eps_2workers_10000conns" 2 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_RAW 100000 20000000 100 gateway_simple_raw_eps_2workers_100000conns" 2 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_RAW 1002 30000000 100 gateway_simple_raw_eps_3workers_1000conns" 3 MODE_GATEWAY_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_RAW 10002 50000000 100 gateway_simple_raw_eps_3workers_10000conns" 3 MODE_GATEWAY_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_RAW 100002 20000000 100 gateway_simple_raw_eps_3workers_100000conns" 3 MODE_GATEWAY_RAW 81 262144"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_RAW 1000 10000000 100 gateway_with_smc_simple_raw_eps_1worker_1000conns" 1 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_RAW 10000 5000000 100 gateway_with_smc_simple_raw_eps_1worker_10000conns" 1 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 1 "MODE_GATEWAY_SMC_RAW 100000 5000000 100 gateway_with_smc_simple_raw_eps_1worker_100000conns" 1 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_RAW 1000 20000000 100 gateway_with_smc_simple_raw_eps_2workers_1000conns" 2 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_RAW 10000 10000000 100 gateway_with_smc_simple_raw_eps_2workers_10000conns" 2 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 2 "MODE_GATEWAY_SMC_RAW 100000 5000000 100 gateway_with_smc_simple_raw_eps_2workers_100000conns" 2 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_RAW 1002 30000000 100 gateway_with_smc_simple_raw_eps_3workers_1000conns" 3 MODE_GATEWAY_SMC_RAW 81 8192"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_RAW 10002 10000000 100 gateway_with_smc_simple_raw_eps_3workers_10000conns" 3 MODE_GATEWAY_SMC_RAW 81 16384"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
::CMD /C "gw_test_bs.bat 3 "MODE_GATEWAY_SMC_RAW 100002 5000000 100 gateway_with_smc_simple_raw_eps_3workers_100000conns" 3 MODE_GATEWAY_SMC_RAW 81 262144"
::IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO All gateway performance tests finished successfully!

staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the performance test! 1>&2
staradmin kill all
EXIT 1