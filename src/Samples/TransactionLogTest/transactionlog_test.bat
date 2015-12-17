:: Some predefined constants.
SET DB_DIR=%~dp0\..\..\.db
SET DB_OUT_DIR=%~dp0\..\..\.db.output
SET DB_NAME=TRANSACTIONLOG
SET TEST_NAME=TransactionLogTest

:: Killing all SC processes.
%~dp0\..\..\staradmin kill all

mkdir %DB_DIR%
mkdir %DB_OUT_DIR%

:: Creating image files.
%~dp0\..\..\sccreatedb.exe -ip %DB_DIR% %DB_NAME%

:: Weaving the test.
%~dp0\..\..\scweaver.exe "%~dp0\%TEST_NAME%.exe" || exit /b 1

:: Starting IPC monitor first.
START CMD /C "%~dp0\..\..\scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Path to signed assembly.
SET TEST_WEAVED_ASSEMBLY=%~dp0\.starcounter\%TEST_NAME%.exe

:: Starting database memory management process.
START CMD /C "%~dp0\..\..\scdata.exe 1 %DB_NAME% %DB_OUT_DIR% %DB_NAME% %DB_DIR% %DB_OUT_DIR%"

:: Starting Prolog process.
START CMD /C "%~dp0\..\..\32bitComponents\scsqlparser.exe 8066"

:: Sleeping some time using ping.
ping -n 5 127.0.0.1 > nul

:: Starting database with some delay.
%~dp0\..\..\sccode.exe 1 %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway || exit /b 1

