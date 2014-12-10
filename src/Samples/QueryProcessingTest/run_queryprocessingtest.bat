
:: Checking if test should be run.
IF "%SC_RUN_QUERYPROCESSING_TEST%"=="False" GOTO :EOF

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=QUERYPROCESSINGTEST
SET TEST_NAME=QueryProcessingTest

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL

:: Checking for existing dirs.
IF EXIST .db (
RMDIR .db /S /Q
RMDIR .db.output /S /Q
)

IF EXIST s\QueryProcessingTest\dumpQueryProcessingDB.sql DEL s\QueryProcessingTest\dumpQueryProcessingDB.sql

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% ( MKDIR %DB_DIR% )
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Creating image files.
sccreatedb.exe -ip %DB_DIR% %DB_NAME%
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Weaving the test.
CALL scweaver.exe "s\%TEST_NAME%\%TEST_NAME%.exe"
IF %ERRORLEVEL% NEQ 0 (
ECHO Error: The query processing regression test failed!
EXIT /b 1
)

:: Starting IPC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Path to signed assembly.
SET TEST_WEAVED_ASSEMBLY=s\%TEST_NAME%\.starcounter\%TEST_NAME%.exe

:: Re-signing the assembly.
"c:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\sn.exe" -R "%TEST_WEAVED_ASSEMBLY%" "..\..\src\Starcounter.snk"
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Starting database memory management process.
START CMD /C "scdata.exe 0 %DB_NAME% %DB_OUT_DIR%" %DB_NAME% %DB_DIR% %DB_DIR%
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Starting log writer process.
START CMD /C "scdblog.exe %DB_NAME% %DB_NAME% %DB_OUT_DIR%"
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Sleeping some time using ping.
ping -n 3 127.0.0.1 > nul
IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Starting database with some delay.
sccode.exe %DB_NAME% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway
IF %ERRORLEVEL% NEQ 0 (
ECHO Error: The query processing regression test failed!
EXIT /b 1
)

sccode.exe %DB_NAME% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway
IF %ERRORLEVEL% NEQ 0 (
ECHO Error: The query processing regression test failed!
EXIT /b 1
) else (
ECHO The query processing regression test succeeded.
EXIT /b 0
)

