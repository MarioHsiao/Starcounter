pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\SqlTest* scdbc.exe -ip .db -lp .db SqlTest
START ScConnMonitor PERSONAL .db.output
scpmm SQLTEST SqlTest .db.output
popd
