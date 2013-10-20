@echo off

for /l %%x in (1, 1, 100) do (

   :: Printing iteration number.
   echo %%x
   
   :: Running one node proxy test.
   run_node_proxy_test.bat
   
   :: Checking exit code.
   IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
)

:: Success message.
ECHO Node loop proxy tests finished successfully!

CMD /C "kill_all.bat" 2>NUL
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the loop proxy test! 1>&2
CMD /C "kill_all.bat" 2>NUL
EXIT 1