
#Add computer domain 
param([Parameter(Mandatory=$false)][String] $Step="0", [Parameter(Mandatory=$true)][String] $EnvironmentName)

Import-Module ServerManager
Import-Module WebAdministration

$global:restartKey = "Service-Machine-Setup"
$global:RegRunKey ="HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
$global:powershell = (Join-Path $env:windir "system32\WindowsPowerShell\v1.0\powershell.exe `
											-executionPolicy Unrestricted")
$ScriptRoot = Split-Path $MyInvocation.MyCommand.Path
$thisScript = "$ScriptRoot\ServiceStartup.ps1"

$ErrorActionPreference = "silentlycontinue"

#Region External Functions
	. "$PSScriptRoot\ConfigurationScript\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions

function Test-Key([string] $path, [string] $key)
{
    return ((Test-Path $path) -and ((Get-Key $path $key) -ne $null))   
}

function Remove-Key([string] $path, [string] $key)
{
	Remove-ItemProperty -path $path -name $key
}

function Set-Key([string] $path, [string] $key, [string] $value) 
{
	Set-ItemProperty -path $path -name $key -value $value
}

function Get-Key([string] $path, [string] $key) 
{
	return (Get-ItemProperty $path).$key
}

function Clear-Any-Restart([string] $key=$global:restartKey) 
{
	if (Test-Key $global:RegRunKey $key) {
		Remove-Key $global:RegRunKey $key
	}
}


function Restart-And-Run([string] $key, [string] $run) 
{
    Set-Key $global:RegRunKey $key $run
    Restart-Computer
    exit
}

function Restart-And-Resume([string] $script, [string] $step, [string] $EnvironmentName) 
{
    Restart-And-Run $global:restartKey "$global:powershell $script -Step $step -EnvironmentName $EnvironmentName "
}

Clear-Any-Restart

function step0()
{

    $requiredRestart = $false
	$windowsfeatures = @("MSMQ", "MSMQ-Services", "MSMQ-Server", "MSMQ-Directory", "MSMQ-Triggers")
	 foreach ($feature in $windowsfeatures)
	 {
	  $state = Get-WindowsFeature -Name $feature.trim()
	  if ($state.Installed -eq $false -and $state.InstallState -ne "InstallPending")
	  {
             $requiredRestart  = $true
	      add-windowsfeature $feature.trim() 
	  }
	 }
    if($requiredRestart -eq $true)
    {
        Write-Host "Step0 MSMQ Installed and Restarting"
	    Restart-And-Resume $thisScript "-Step 1" -EnvironmentName $EnvironmentName
    }
    else
    {
        Write-Host "After Reboot Step0 MSMQ Installed and Going Step1"
        return $true
    }
}


function step1( [Parameter(Mandatory=$false)][String]$domain = "d9.aria.local",
				[Parameter(Mandatory=$false)][String]$username = "ULAdmin",
				[Parameter(Mandatory=$false)][string]$password = "ALLAccount$"
			  )
{
	if ((gwmi win32_computersystem).partofdomain -eq $true) {
        Write-Host "After Active Directory Installed and Going Step 2"
        return $true

	} else {
		$password1 = $password | ConvertTo-SecureString -asPlainText -Force
		$username = "$domain\$username" 
		$credential = New-Object System.Management.Automation.PSCredential($username,$password1)
		Add-Computer -DomainName $domain -Credential $credential
          Write-Host "Step1 Added to Active Directory  and Going Restart"
		Restart-And-Resume $thisScript "-Step 2" -EnvironmentName $EnvironmentName
		}
}

function step2(	[Parameter(Mandatory=$false)][String]$username = "Web_Aria",
				[Parameter(Mandatory=$false)][string]$password = "th3_f0rce")
{
	$webSites = Get-ChildItem IIS:\Sites 
	$poolName  = $webSites | Where{$_.Name -like "UL.Aria*" }  | Select-Object applicationPool
	$apppoolid =  $poolName.applicationPool
     $domain =  Get-WindowsNtDomain
	 $username = "$domain\$username" 
    if($apppoolid)
    {
		
		Set-ItemProperty "IIS:\AppPools\$apppoolid" -name processModel -value @{userName=$username;password=$password;identitytype=3}
    }
    else
    {
         Write-Host "Error - Script executed before app pool created. You need to modify script for wait"
    }

	.$ScriptRoot\GrantReadPermissionOnCertificate.ps1 -User $username -Subject 'CN=SP.contoso.com'
	Write-Host "Step2 IIS AppPool Updated Done and going to Step3"
	
	return $true

}


function step3(
[Parameter(Mandatory=$true)][string]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName
)
{
    .$ScriptRoot\Configure.ps1 -EnvironmentName $EnvironmentName 
	 Write-Host "Step3 Done and ALL IS WELL"
	
}

function Main([Parameter(Mandatory=$false)][String]$Step = 0,[Parameter(Mandatory=$true)][String] $EnvironmentName,[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName)
{

    #$EnvironmentName ="DevInt"
    

    $EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername
    $basePath = "$PSScriptRoot\ConfigurationScript"
    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName -XMLDirectory  $basePath
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName
    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName $EnvironmentConfigurationFileName $EnvironmentName $ServerName $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory  $basePath
	$domainName = $EnvironmentConfiguration.DomainName


    if($Step -eq "0")
    {
        $result = step0
        if($result -eq $true)
            {
               Main -Step 1 -EnvironmentName $EnvironmentName
            }
    }
    elseif($Step -eq "1")
    { 
        $result = step1 -domain $domainName 
		#-username  $EnvironmentConfiguration.Accounts[0].Username -password $EnvironmentConfiguration.Accounts[0].Password
        if($result -eq $true)
         {
              Main -Step 2 -EnvironmentName $EnvironmentName
         }

     }
    elseif($Step -eq "2")
    {
     
     $result = step2 -username  $EnvironmentConfiguration.IisSiteUserName -password $EnvironmentConfiguration.IisSiteUserPwd
     if($result -eq $true)
        {
             Main -Step 3 -EnvironmentName $EnvironmentName
        }
     }
    elseif($Step -eq "3")
    {
        return step3 -EnvironmentName $EnvironmentName
    }
    else
    {
        Write-Host "No step number"
    }
	 Exit
}


Main -Step $Step -EnvironmentName $EnvironmentName





