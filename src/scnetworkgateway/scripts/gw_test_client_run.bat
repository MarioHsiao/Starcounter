staradmin kill all

timeout 1

START CMD /C "scipcmonitor.exe PERSONAL .db.output"

START CMD /C "scnetworkgateway.exe personal gw_test_pair_client.xml .db.output"

timeout 1

START CMD /C "sccode.exe MYDB_CLIENT --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath=s\NetworkIoTest\NetworkIoTest.exe --FLAG:NoDb --GatewayWorkersNumber=1 --UserArguments="DbNumber=0 PortNumber=81 TestType=MODE_SMC_HTTP_ECHO""