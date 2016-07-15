$ScriptRoot = Split-Path $MyInvocation.MyCommand.Path
#function Enable-AspnetState {

function Enable-ASPNETSTATESERVICE {
	$AdminKey = "HKLM:\SYSTEM\CurrentControlSet\Services\aspnet_state\Parameters"
	#$UserKey = "HKLM:\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}"
	Set-ItemProperty -Path $AdminKey -Name "AllowRemoteConnection" -Value 1
	$serviceName = "aspnet_state"
	Set-Service -Name $serviceName -StartupType Automatic
	Start-Service -Name $serviceName
	Write-Host "IE Enhanced Security Configuration (ESC) has been enabled." -ForegroundColor Green
}

Enable-ASPNETSTATESERVICE