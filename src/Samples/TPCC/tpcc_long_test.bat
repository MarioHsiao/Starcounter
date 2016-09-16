%~dp0\..\..\star.exe @@CreateRepo .srv

if not "%1"=="noclean" (
	rem kill dirty db instance if exists
	call %~dp0\..\..\kill_all.bat
	%~dp0\..\..\staradmin start server
	%~dp0\..\..\staradmin -database=TPCC delete --force db
)

call %~dp0\..\..\kill_all.bat
%~dp0\..\..\star -D=TPCC %~dp0\tpcc.exe || exit /b 1

if not "%1"=="noclean" (
	rem populate data
	powershell -command "Invoke-RestMethod http://localhost:8080/populate -timeoutsec 500" || exit /b 1
)

rem execute transactions

rem Delivery
powershell -command "Invoke-RestMethod http://localhost:8080/gen/delivery?W_ID=1'"^&'"D_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/delivery -method POST" || exit /b 1

rem New Order
powershell -command "Invoke-RestMethod http://localhost:8080/gen/neworder?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/neworder -method POST" || exit /b 1

rem Order Status
powershell -command "Invoke-RestMethod http://localhost:8080/gen/orderstatus?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/orderstatus -method POST" || exit /b 1

rem Payment
powershell -command "Invoke-RestMethod http://localhost:8080/gen/payment?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/payment -method POST" || exit /b 1

rem Delivery
powershell -command "Invoke-RestMethod http://localhost:8080/gen/stocklevel?W_ID=1'"^&'"D_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/stocklevel -method POST" || exit /b 1


rem run transactions internally
powershell -command "Invoke-RestMethod http://localhost:8080/all_no_io?load_factor=1 -method POST" || exit /b 1

rem run transactions using localhost I/O
%~dp0\tpcc.exe -client 1000 || exit /b 1

powershell -command "Invoke-RestMethod http://localhost:8080/stat"