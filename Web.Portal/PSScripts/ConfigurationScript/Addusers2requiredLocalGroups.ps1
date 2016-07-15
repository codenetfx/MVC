<#
.SYNOPSIS
	Adds Active Directory user ids to local server groups based on
	 the server role
	 
.PARAMETER ServerRole
	Server type (AD, SQL, SPWFE, SPAPP).

.DESCRIPTION
	The application requires a set of user names in AD before the 
	application can be installed.
#>
param(
[Parameter(Mandatory=$false)][String]$ServerRole,
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2

#Region External Functions
	. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
#EndRegion External Functions

function AddUserToLocalGroup(
[Parameter(Mandatory=$true)][String]$GroupName,
[Parameter(Mandatory=$true)][String]$UserName
)
{
        $IsUserExists = net localgroup $GroupName | Where {$_ -eq $UserName }
        if(!$IsUserExists)
        {
		    net localgroup $GroupName /add $UserName
        }
}

function Main(
[Parameter(Mandatory=$false)][String]$ServerRole,
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$false)][String]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)
{

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername

    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

	$EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles    
	$EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory  $XMLDirectory

    $ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
	Write-Host "`$ServerRole:  $ServerRole"
	
	$netbiosname = Get-WindowsNtDomain
	
	if (($ServerRole -eq "Portal") -or ($ServerRole -eq "Services"))
	{ 
        AddUserToLocalGroup -GroupName "Administrators" -UserName "$netbiosname\Web_Aria"
	}

	if (($ServerRole -eq "SPWFE") -or ($ServerRole -eq "SPAPP"))
	{ 
		# SharePoint Install Account
        AddUserToLocalGroup -GroupName "Administrators" -UserName "$netbiosname\SP_Install"
		

		# Required to write to the SharePoint ULS log
         AddUserToLocalGroup -GroupName 'Performance Log Users' -UserName "$netbiosname\SP_PortalAppPool"
         AddUserToLocalGroup -GroupName 'Performance Log Users' -UserName "$netbiosname\SP_SearchService"
         AddUserToLocalGroup -GroupName 'Performance Log Users' -UserName "$netbiosname\SP_Servicess"
	}
}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory  $XMLDirectory