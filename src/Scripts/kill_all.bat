:: Killing all Starcounter-related processes if any are running.
taskkill /f /t /im DaemonObserver*
taskkill /f /t /im ScConnMonitor*
taskkill /f /t /im Postsharp*
taskkill /f /t /im Starcounter*
taskkill /f /t /im Scdbs*
taskkill /f /t /im Scpmm.exe
taskkill /f /t /im x86_64-w64-mingw32-gcc.exe
taskkill /f /t /im ActivityMonitorServer.exe
taskkill /f /t /im ServerLogTail.exe
taskkill /f /t /im ScNetGateway.exe
taskkill /f /t /im LoadAndLatencyClient.exe
taskkill /f /t /im PolePositionClient.exe
taskkill /f /t /im SQLTestClient.exe
taskkill /f /t /im SQLTest1Client.exe
taskkill /f /t /im SQLTest2Client.exe
taskkill /f /t /im SQLTest3Client.exe
taskkill /f /t /im Boot.exe