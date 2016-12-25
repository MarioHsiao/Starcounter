param([string]$bin_path, [string]$db_name, [string]$db_dir, [string]$log_dir, [string]$db_uuid)
$scdatapath = Join-Path -Path $bin_path -ChildPath "scdata.exe"
$scdata = Start-Process $scdatapath """{ \""eventloghost\"": \""$($db_name)\"", \""eventlogdir\"": \""$($log_dir)\"", \""databasename\"": \""$($db_name)\"", \""databasedir\"": \""$($db_dir)\"" }""" -PassThru -WindowStyle Minimized

$MethodDefinition = @’

            [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
            public static extern int WaitNamedPipe(string lpNamedPipeName, int nTimeOut);

‘@

$kernel32=Add-Type -MemberDefinition $MethodDefinition -Name ‘Kernel32’ -Namespace ‘Win32’ -PassThru

ForEach ($retry in 1..100 )
{
	if ( $Kernel32::WaitNamedPipe("\\.\pipe\STAR_P_$($db_uuid)",-1) -eq 1)
        {
		exit 0
	}

        if ( $scdata.HasExited )
	{
		Write-Host "scdata unexpectedly terminated"
		exit 1
	}

	Start-Sleep -m 50
}

Write-Host "scdata hasn't started up in time"
exit 1

