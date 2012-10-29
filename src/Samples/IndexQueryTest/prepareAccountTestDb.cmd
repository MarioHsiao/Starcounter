pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\AccountTest* scdbc.exe -ip .db -lp .db AccountTest
START ScConnMonitor PERSONAL .db.output
START scpmm ACCOUNTTEST AccountTest .db.output
popd
