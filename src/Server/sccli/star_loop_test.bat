@echo off

:: Checking if test should be run.
IF "%SC_RUN_STAR_LOOP_TEST%"=="False" GOTO :EOF

:: Checking if number of cycles parameter is supplied.
set LOOP_TIMES=%1
IF "%LOOP_TIMES%"=="" SET LOOP_TIMES=5
ECHO Test is going to loop %LOOP_TIMES% times:

staradmin kill all

for /l %%x in (1, 1, %LOOP_TIMES%) do (

   :: Printing iteration number.
   echo %%x
   
   :: Starting NetworkIOTest
   star.exe "%StarcounterBin%\s\NetworkIoTest\NetworkIoTest.exe" DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS || GOTO TESTFAILED
)

:: Success message.
ECHO Star.exe loop tests finished successfully!

staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the star.exe loop test! 1>&2
staradmin kill all
EXIT /b 1