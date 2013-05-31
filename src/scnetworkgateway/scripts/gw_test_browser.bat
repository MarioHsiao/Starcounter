:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=MYDB

:: Killing all processes.
CMD /C "kill_all.bat" 2>NUL

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% (
MKDIR %DB_DIR%
sccreatedb.exe -ip %DB_DIR% -lp %DB_DIR% %DB_NAME%
)
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Starting IPC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Starting database memory management process.
START CMD /C "scdata.exe %DB_NAME% %DB_NAME% %DB_OUT_DIR%"

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"

:: Starting database with some delay.
START CMD /C "timeout 2 && sccode.exe %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath=NetworkIoTest\NetworkIoTest.exe --SchedulerCount=2 --ChunksNumber=8192 --UserArguments="DbNumber=0 PortNumber=80 TestType=MODE_STANDARD_BROWSER""

:: Starting network gateway.
scnetworkgateway.exe personal scnetworkgateway.xml %DB_OUT_DIR%
