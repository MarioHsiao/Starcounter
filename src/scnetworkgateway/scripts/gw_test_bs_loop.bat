:: Executing some number of same test.
for /l %%x in (1, 1, 100) do (
   gw_test_bs.bat "5 MODE_GATEWAY_RAW 1000 50000000 100 gateway_simple_raw_eps_3workers_1000conns" 1 MODE_GATEWAY_RAW 81 8192
   IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
)

:: Killing all processes.
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the performance test! 1>&2
EXIT 1

