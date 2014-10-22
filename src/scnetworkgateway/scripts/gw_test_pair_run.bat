staradmin kill all

timeout 1

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

START CMD /C "scnetworkgateway.exe personal gw_test_pair_server.xml .db.output"

START CMD /C "scnetworkgateway.exe personal gw_test_pair_client.xml .db.output"

timeout 1

START CMD /C "sccode.exe MYDB_SERVER --OutputDir=.db.output --TempDir=.db.output --GatewayWorkersNumber=1 --AutoStartExePath=s\NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --UserArguments="DbNumber=0 PortNumber=80 TestType=MODE_SMC_HTTP_ECHO""

timeout 1

START CMD /C "sccode.exe MYDB_CLIENT --OutputDir=.db.output --TempDir=.db.output --GatewayWorkersNumber=1 --AutoStartExePath=s\NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --UserArguments="DbNumber=0 PortNumber=81 TestType=MODE_SMC_HTTP_ECHO""
