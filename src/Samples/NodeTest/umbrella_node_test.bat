ECHO Performance Tests: Node HTTP1.
CALL node_test.bat -ProtocolType=ProtocolHttpV1 -AsyncMode=ModeRandom
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Node HTTP1 Big Data Transfers.
CALL node_test.bat -NumEchoesPerWorker=5000 -ProtocolType=ProtocolHttpV1 -AsyncMode=ModeRandom -MaxEchoBytes=50000
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Node HTTP1 Simple Aggregation.
CALL node_test.bat -MaxEchoBytes=1000 -ProtocolType=ProtocolHttpV1 -UseAggregation=True -NumEchoesPerWorker=100000
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Node HTTP1 Aggregation Big Data Transfers.
CALL node_test.bat -MaxEchoBytes=30000 -ProtocolType=ProtocolHttpV1 -UseAggregation=True -NumEchoesPerWorker=10000
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Raw sockets tests.
CALL node_test.bat -AsyncMode=ModeSync -ProtocolType=ProtocolRawPort
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Node WebSockets.
CALL node_test.bat -ProtocolType=ProtocolWebSockets -AsyncMode=ModeSync -NumEchoesPerWorker=10000
IF ERRORLEVEL 1 GOTO FAILED

ECHO Performance Tests: Node WebSockets Big Data Transfers.
CALL node_test.bat -NumEchoesPerWorker=5000 -ProtocolType=ProtocolWebSockets -AsyncMode=ModeSync -MaxEchoBytes=50000
IF ERRORLEVEL 1 GOTO FAILED

:: Tests finished successfully.
GOTO :EOF

:: If we are here than some test has failed.
:FAILED

EXIT /b %ERRORLEVEL%
