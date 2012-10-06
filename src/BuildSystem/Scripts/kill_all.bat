:: Killing all Starcounter-related processes if any are running.
TASKKILL /f /t /im Boot.exe
TASKKILL /f /t /im Scpmm.exe
TASKKILL /f /t /im ScGateway.exe
TASKKILL /f /t /im ScConnMonitor.exe

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
:: TASKKILL /f /t /im ScCode.exe
:: TASKKILL /f /t /im ScData.exe
:: TASKKILL /f /t /im ScMonitor.exe
:: TASKKILL /f /t /im ScServer.exe

IF NOT "%SC_RUNNING_ON_BUILD_SERVER%"=="" EXIT 0