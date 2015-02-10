FOR /f %%i IN ('star.exe --version') DO SET Output=%%i

SET Version=%Output:~8%

IF "%Version%"=="" GOTO TESTFAILED

:: Success message.
ECHO Version check test finished successfully!
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during version check test! 1>&2

EXIT /b 1