pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if exist .db\SqlTest* call ..\..\src\Samples\SQLTest\cleanSqlTestDb.cmd
call scdbc.exe -ip .db -lp .db SqlTest
START ScConnMonitor PERSONAL .db.output
scpmm SQLTEST SqlTest .db.output
popd
