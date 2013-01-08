pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\BindingTest* sccreatedb.exe -ip .db -lp .db BindingTest
START scipcmonitor PERSONAL .db.output
START scdata BINDINGTEST BindingTest .db.output
popd
