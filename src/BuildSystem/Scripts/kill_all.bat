:: Killing all Starcounter-related processes if any are running.
TASKKILL /f /t /im sccode.exe
TASKKILL /f /t /im scdata.exe
TASKKILL /f /t /im scnetworkgateway.exe
TASKKILL /f /t /im scnetworkgatewayloopedtest.exe
TASKKILL /f /t /im scipcmonitor.exe
TASKKILL /f /t /im scweaver.exe
TASKKILL /f /t /im scsqlparser.exe

:: TASKKILL /f /t /im DaemonObserver.exe
:: TASKKILL /f /t /im Postsharp*
:: TASKKILL /f /t /im Starcounter*
:: TASKKILL /f /t /im Scdbs*
:: TASKKILL /f /t /im x86_64-w64-mingw32-gcc.exe
:: TASKKILL /f /t /im ActivityMonitorServer.exe
:: TASKKILL /f /t /im ServerLogTail.exe
:: TASKKILL /f /t /im LoadAndLatencyClient.exe
:: TASKKILL /f /t /im PolePositionClient.exe
:: TASKKILL /f /t /im SQLTestClient.exe
:: TASKKILL /f /t /im SQLTest1Client.exe
:: TASKKILL /f /t /im SQLTest2Client.exe
:: TASKKILL /f /t /im SQLTest3Client.exe

:: Sleeping some time using ping.
ping -n 3 127.0.0.1 > nul

IF NOT "%SC_RUNNING_ON_BUILD_SERVER%"=="" EXIT 0