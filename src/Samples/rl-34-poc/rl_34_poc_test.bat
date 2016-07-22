rem %~dp0\..\..\star.exe @@CreateRepo .srv
rem COPY /Y %~dp0\..\..\scnetworkgateway.sample.xml %~dp0\..\..\.srv\personal\scnetworkgateway.xml

if not "%1"=="noclean" (
	rem kill dirty db instance if exists
	call kill_all.bat
	staradmin start server
	staradmin -database=rl34poc delete --force db
)

call kill_all.bat
star -D=rl34poc %~dp0\rl_34_poc.exe || exit /b 1

rem create checklist
powershell -command "Invoke-RestMethod http://localhost:8080/newchecklist?name=checklist1 -method POST | ConvertTo-Json" || exit /b 1

rem create first item in checklist
powershell -command "Invoke-RestMethod http://localhost:8080/newentry?checklist=checklist1'"^&'"item=item1 -method POST | ConvertTo-Json" || exit /b 1

rem create second item in checklist
powershell -command "Invoke-RestMethod http://localhost:8080/newentry?checklist=checklist1'"^&'"item=item2 -method POST | ConvertTo-Json" || exit /b 1

rem mark first item as done
powershell -command "Invoke-RestMethod http://localhost:8080/markentrydone?checklist=checklist1'"^&'"item=item1 -method POST | ConvertTo-Json" || exit /b 1


rem close checklist
powershell -command "Invoke-RestMethod http://localhost:8080/closechecklist?name=checklist1 -method POST | ConvertTo-Json" || exit /b 1


rem mark second item as done
powershell -command "Invoke-RestMethod http://localhost:8080/markentrydone?checklist=checklist1'"^&'"item=item2 -method POST | ConvertTo-Json" || exit /b 1

rem show checklist history from the log. only first item should be marked as done
powershell -command "Invoke-RestMethod http://localhost:8080/checkliststateonfirstclose?name=checklist1 | ConvertTo-Json" || exit /b 1

rem cheat history field in database
powershell -command "Invoke-RestMethod http://localhost:8080/cheatchecklist?name=checklist1 -method POST | ConvertTo-Json" || exit /b 1

rem show checklist history from the log. ignore cheated history field
powershell -command "Invoke-RestMethod http://localhost:8080/checkliststateonfirstclose?name=checklist1 | ConvertTo-Json" || exit /b 1

rem delete checklist
powershell -command "Invoke-RestMethod http://localhost:8080/deletechecklist?name=checklist1 -method DELETE | ConvertTo-Json" || exit /b 1

rem create new checklist with the same name
powershell -command "Invoke-RestMethod http://localhost:8080/newchecklist?name=checklist1 -method POST | ConvertTo-Json" || exit /b 1

rem show checklist history from the log. ignore new checklist
powershell -command "Invoke-RestMethod http://localhost:8080/checkliststateonfirstclose?name=checklist1 | ConvertTo-Json" || exit /b 1
