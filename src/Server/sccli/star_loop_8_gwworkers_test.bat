@echo off

SET SC_GW_WORKERS_NUMBER=8
ECHO Starting test with number of gateway workers: %SC_GW_WORKERS_NUMBER%
SET SC_TIMESTAMPS_FILE_PATH=star_loop_8_gwworkers_test_log.txt

for /l %%x in (1, 1, 30) do (

   :: Printing iteration number.
   echo %%x
   
   :: Starting NetworkIOTest
   star.exe --logsteps "%StarcounterBin%\s\NetworkIoTest\NetworkIoTest.exe" DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS
   
   :: Checking exit code.
   IF ERRORLEVEL 1 GOTO TESTFAILED
)

:: Success message.
ECHO Test finished successfully!
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2

EXIT /b 1