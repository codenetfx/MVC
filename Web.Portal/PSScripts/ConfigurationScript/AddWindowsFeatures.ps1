<#
.SYNOPSIS
	Install additional Windows Features based on server role.

.DESCRIPTION
	PowerShell-ISE
	SMTP-Server
	MSMQ
	IIS
#>
param(
[Parameter(Mandatory=$false)][String]$ServerRole,
[Parameter(Mandatory=$false)][String]$EnvironmentName,
[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2

function Get-FeaturesToInstall
{
	param(
				[Parameter(Mandatory=$true)][String]$ServerRole
		)

	
		[array]$featuresToInstall =$null
		
		if (($ServerRole -in @("Portal", "Services")))
		{
			$featuresToInstall += @("Web-Common-Http","Web-Asp-Net","Web-Net-Ext","Web-ISAPI-Ext","Web-ISAPI-Filter","Web-Http-Logging","Web-Request-Monitor","Web-Basic-Auth","Web-Windows-Auth","Web-Filtering","Web-Performance","Web-Mgmt-Console","Web-Mgmt-Compat","WAS")
			$os = gwmi Win32_OperatingSystem
			if($os.Version.StartsWith("6.1") -or $os.Version.StartsWith("6.0"))
			{
				$featuresToInstall += ,"RSAT-Web-Server"
			}
			else
			{
			    #Assume the server is 2012 or later.  It'll fail if there's a feature name change.
				$featuresToInstall += "Web-Server", "NET-Framework-45-Core", "NET-Framework-45-ASPNET"
			}				
		}
		
		#MSMQ is used with Enterprise Library Logging
		#VSappidi - Removed Services MSMQ.
		if ($ServerRole -in @("Portal", "MSMQ",  "OnPrem"))
		{
			if ((Get-WindowsFeature -Name "MSMQ").Installed -eq $false)
		    {
				$featuresToInstall += "MSMQ"			
            }
		}

		if ($ServerRole -in @("Services"))
		{
			if ((Get-WindowsFeature -Name "SMTP-Server" ).Installed -eq $false)
		    {
				$featuresToInstall += "SMTP-Server"
			}
		}

		return $featuresToInstall
}


function Main()
{
	param(
	[Parameter(Mandatory=$false)][String]$ServerRole,
	[Parameter(Mandatory=$false)][String]$EnvironmentName,
	[Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][String]$XMLDirectory
	)

	#Region External Functions
		. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
	#EndRegion External Functions

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername
		
	$GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
	$GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

	$EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

	$EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles
	$EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

	$ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
	Write-Host "`$ServerRole:  $ServerRole"

	# --------------------------------------------------------------------
	# Loading Feature Installation Modules
	# --------------------------------------------------------------------
	Import-Module ServerManager 

	if ($ServerRole -in @("Portal", "Services"))
	{
		$features = Get-FeaturesToInstall $ServerRole
		foreach($feature in $features)
		{
			if ((Get-WindowsFeature -Name $feature).Installed -eq $false)
	        {				
				Write-Host (Get-Date)"Installing Windows feature $feature"
				Add-WindowsFeature -Name $feature -IncludeAllSubFeature			
			}
			else
			{
				Write-Host (Get-Date)"Skipping $feature feature installation since it's already installed."
			}	
		}
	}
}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory
