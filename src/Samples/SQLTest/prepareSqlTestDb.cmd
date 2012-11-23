pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db (
mkdir .db
mkdir .db.output
)
if exist .db\SqlTest* call ..\..\src\Samples\SQLTest\cleanSqlTestDb.cmd
call sccreatedb.exe -ip .db -lp .db SqlTest
START scipcmonitor PERSONAL .db.output
START scdata SQLTEST SqlTest .db.output
popd
