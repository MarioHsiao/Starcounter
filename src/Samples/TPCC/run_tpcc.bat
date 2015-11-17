%~dp0\..\..\star.exe @@CreateRepo .srv
COPY /Y %~dp0\..\..\scnetworkgateway.sample.xml %~dp0\..\..\.srv\personal\scnetworkgateway.xml

if not "%1"=="noclean" (
	rem kill dirty db instance if exists
	call %~dp0\..\..\kill_all.bat
	%~dp0\..\..\staradmin start server
	%~dp0\..\..\staradmin -database=TPCC delete --force db
)

call %~dp0\..\..\kill_all.bat
%~dp0\..\..\star -D=TPCC %~dp0\tpcc.exe || exit /b

if not "%1"=="noclean" (
	rem populate data
	powershell -command "Invoke-RestMethod http://localhost:8080/populate" || exit /b
)

rem execute transactions

rem Delivery
powershell -command "Invoke-RestMethod http://localhost:8080/gen/delivery?W_ID=1'"^&'"D_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/delivery -method POST" || exit /b

rem New Order
powershell -command "Invoke-RestMethod http://localhost:8080/gen/neworder?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/neworder -method POST" || exit /b

rem Order Status
powershell -command "Invoke-RestMethod http://localhost:8080/gen/orderstatus?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/orderstatus -method POST" || exit /b

rem Payment
powershell -command "Invoke-RestMethod http://localhost:8080/gen/payment?W_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/payment -method POST" || exit /b

rem Delivery
powershell -command "Invoke-RestMethod http://localhost:8080/gen/stocklevel?W_ID=1'"^&'"D_ID=1 | ConvertTo-Json | Invoke-RestMethod http://localhost:8080/do/stocklevel -method POST" || exit /b


rem run transactions internally
powershell -command "Invoke-RestMethod http://localhost:8080/all_no_io?load_factor=1 -method POST" || exit /b


powershell -command "Invoke-RestMethod http://localhost:8080/stat"