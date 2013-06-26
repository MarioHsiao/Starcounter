START CMD /C "kill_all.bat"

timeout 1

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

START CMD /C "scnetworkgateway.exe personal gw_test_pair_server.xml .db.output"

timeout 1

START CMD /C "sccode.exe MYDB_SERVER --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath=s\NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --UserArguments="DbNumber=0 PortNumber=80 TestType=MODE_SMC_HTTP_ECHO""