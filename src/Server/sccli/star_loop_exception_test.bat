@echo off

:: Checking if test should be run.
IF "%SC_RUN_STAR_LOOP_EXCEPTION_TEST%"=="False" GOTO :EOF

:: Checking if number of cycles parameter is supplied.
set LOOP_TIMES=%1
IF "%LOOP_TIMES%"=="" SET LOOP_TIMES=100
ECHO Test is going to loop %LOOP_TIMES% times:

staradmin -killall

:: Creating repository if it does not exist.
IF NOT EXIST ".srv" star.exe @@CreateRepo .srv
COPY /Y scnetworkgateway.xml .srv\personal\scnetworkgateway.xml

:: Setting StarcounterBin as current directory.
SET StarcounterBin=%CD%

for /l %%x in (1, 1, %LOOP_TIMES%) do (

   :: Printing iteration number.
   echo %%x
   
   star s\NetworkIoTest\NetworkIoTest.exe DbNumber=1 PortNumber=8080 TestType=MODE_THROW_EXCEPTION
)

:: Success message.
ECHO Star.exe loop exception test finished successfully!

:: Killing all processes.
staradmin -killall
GOTO :EOF
