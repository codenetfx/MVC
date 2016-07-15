<#
.SYNOPSIS
	Creates SQL Server Aliases based on the server role type.

.PARAMETER ServerRole
	Valid choices are Portal, Services, SPWFE, SPAPP
	
.PARAMETER SqlServerName
	Name of the SQL Server
	
.PARAMETER SQLServerInstanceNameSharePoint
	Name of the SQL Server instance used by SharePoint
	
.NOTES
	All SQL Aliases will use Protocol=TCP/IP
#>
param
(
    [Parameter(Mandatory=$false)][string]$ServerRole,
    [Parameter(Mandatory=$false)][string]$EnvironmentName,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][string]$XMLDirectory
)

Set-StrictMode -Version 2

#Region External Functions
	. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
	. "$PSScriptRoot\Common\SqlServerAliasConfigurationFunctions.ps1"
#EndRegion External Functions

function EnsureSqlAlias([Parameter(Mandatory=$false)][string]$Server, [Parameter(Mandatory=$true)][string]$Name, [Parameter(Mandatory=$true)][string]$SqlServerName, [Parameter(Mandatory=$false)][string]$SqlInstance, [Parameter(Mandatory=$false)][string]$Port, [Parameter(Mandatory=$false)][string]$Protocol) 
{

    #$aria = get-sqlalias | Where-Object { $_.Name -eq $Name }

    #if($aria -eq $null) {
        Write-Host "Creating SQL Alias '$Name'" -ForegroundColor Green
        add-sqlalias -Server $Server -Name $Name -SqlServerName $SqlServerName -SqlInstance $SqlInstance -Port $Port -Protocol $Protocol
    #}
    #else {
    #    Write-Host "SQL Alias '$ariaAlias' found" -ForegroundColor Yellow
    #}
}

function Main(
    [Parameter(Mandatory=$false)][string]$ServerRole,
    [Parameter(Mandatory=$false)][string]$EnvironmentName,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][string]$XMLDirectory
)
{

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername

    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles    
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

    $ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
    	
	#SQL Aliases will be based on the first SQL Server specified in the configuration.
	[string]$SqlServerName = $EnvironmentConfiguration.SQLServerNameSharePoint
	[string]$SQLServerInstanceNameSharePoint = $EnvironmentConfiguration.SQLServerInstanceNameSharePoint
	
        foreach ($SQLAlias in $EnvironmentConfiguration.SQLAliases)
        {
            EnsureSqlAlias -Name $SQLAlias.Name -SqlServerName $SQLAlias.SQLServerName -SqlInstance $SQLAlias.SQLInstance -Port $SQLAlias.Port -Protocol $SQLAlias.Protocol
        }
}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory