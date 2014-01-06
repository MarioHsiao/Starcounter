@echo off

:: Checking if test should be run.
IF "%SC_RUN_STAR_LOOP_TEST%"=="False" GOTO :EOF

:: Checking if number of cycles parameter is supplied.
set LOOP_TIMES=%1
IF "%LOOP_TIMES%"=="" SET LOOP_TIMES=1000
ECHO Test is going to loop %LOOP_TIMES% times:


for /l %%x in (1, 1, %LOOP_TIMES%) do (

   :: Printing iteration number.
   echo %%x
   
   :: Starting NetworkIOTest
   NodeTest.exe %*
)

:: Success message.
ECHO Star.exe loop tests finished successfully!

::staradmin -killall
GOTO :EOF
