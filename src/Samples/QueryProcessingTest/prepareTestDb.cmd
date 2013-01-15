pushd C:\GitRepositories\Starcounter\Level1\bin\Debug
if not exist .db\QueryProcessingTest* sccreatedb.exe -ip .db -lp .db QueryProcessingTest
START scipcmonitor PERSONAL .db.output
START scdata QUERYPROCESSINGTEST QueryProcessingTest .db.output
popd
