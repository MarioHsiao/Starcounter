pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\AccountTest* scdbc.exe -ip .db -lp .db AccountTest
scpmm ACCOUNTTEST AccountTest .db.output
popd
