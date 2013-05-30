pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\AccountTest* sccreatedb.exe -ip .db -lp .db AccountTest
START scipcmonitor PERSONAL .db.output
START scdata ACCOUNTTEST AccountTest .db.output
START 32bitComponents\scsqlparser.exe 8066
popd
