<#
.SYNOPSIS 
	Configure the Windows File Rules

.PARAMETER ServerRole
	Server type (AD, SQL, SPWFE, SPAPP).

.DESCRIPTION
	Some firewall rules are common to all role types.  Other rules vary byased server type
	If using Windows Server 2012, replace the netsh advfirewall commands with cmdlets in the NetworkSecurity module.

	The firewall rule names specified within this script are the US English localized names.

	Rules created by Windows Default on all the servers.
		World Wide Web Services (HTTPS Traffic-In) - Profile=All.  This should already be enabled by default.
		World Wide Web Services (HTTP Traffic-In) - Profile=All.  This should already be enabled by default

.NOTES
	SharePoint creates the same default rules on both the SPWFE and SPAPP:
		Portal - TCP 80 Profile=All.  
		MySite Host - TCP 80 Profile=All.
		SharePoint Central Administration v4 - Port 2010 Profile=All.
		SharePoint Search - TCP Ports - 16500, 160501, 16502, etc. Profile=All.
		SharePoint Web Services - TCP 32843, 32844, 32845 Profile=All.
		SPUserCodeV4 - TCP 32846 Profile=All.
#>
param
(
	[Parameter(Mandatory=$false)]$EnvironmentName,
	[Parameter(Mandatory=$false)]$ServerRole,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][String]$XMLDirectory
)

Set-StrictMode -Version 2

#Region External Functions
	. "$PSScriptRoot\Common\ConfigurationScriptFunctions.ps1"
#EndRegion

function EnableInboundFirewallRule([string]$RuleName, [string]$Profile)
{
    Write-Host "Enabling Inbound Firewall Rule $RuleName on $Profile"
	netsh advfirewall firewall add rule name=$RuleName dir=in action=allow enable=yes profile=$Profile
}

function EnableOutboundFirewallRule([string]$RuleName, [string]$Profile)
{
    Write-Host "Enabling Outbound Firewall Rule $RuleName on $Profile"
	netsh advfirewall firewall add rule name=$RuleName dir=out enable=yes profile=$Profile
}

function CreateAllPortsRules()
{
    Write-Debug "Creating All Ports rules - TCP"
	netsh advfirewall firewall add rule name="All Ports TCP" dir=out action=allow localport=Any Protocol=tcp enable=yes profile=any
	netsh advfirewall firewall add rule name="All Ports TCP" dir=in action=allow localport=Any Protocol=tcp enable=yes profile=any

    Write-Debug "Creating All Ports rules - UDP"
	netsh advfirewall firewall add rule name="All Ports UDP" dir=out action=allow localport=Any Protocol=udp enable=yes profile=any
	netsh advfirewall firewall add rule name="All Ports UDP" dir=in action=allow localport=Any Protocol=udp enable=yes profile=any
}

function EnableFirewallRulesForOtherSqlServices()
{
		#netsh advfirewall firewall add rule name="Open Port 80" dir=in action=allow protocol=TCP localport=80

		#Write-Host "Enabling Conventional SQL Server Service Broker port 4022"
		#netsh advfirewall firewall add rule name="SQL Service Broker" dir=in action=allow protocol=TCP localport=4022

		#Write-Host "Enabling Transact SQL/RPC port 135"
		#netsh advfirewall firewall add rule name="SQL Debugger/RPC" dir=in action=allow protocol=TCP localport=135

		#Write-Host "=========  Analysis Services Ports  =============="
		#Write-Host "Enabling SSAS Default Instance port 2383"
		#netsh advfirewall firewall add rule name="Analysis Services" dir=in action=allow protocol=TCP localport=2383

		#Write-Host "Enabling SQL Server Browser Service port 2382"
		#netsh advfirewall firewall add rule name="SQL Browser" dir=in action=allow protocol=TCP localport=2382

		#Write-Host "=========  Misc Applications  =============="
		#Write-Host "Enabling HTTP port 80"
		#netsh advfirewall firewall add rule name="HTTP" dir=in action=allow protocol=TCP localport=80
		#Write-Host "Enabling SSL port 443"
		#netsh advfirewall firewall add rule name="SSL" dir=in action=allow protocol=TCP localport=443
		#Write-Host "Enabling port for SQL Server Browser Service's 'Browse' Button"
		#netsh advfirewall firewall add rule name="SQL Browser" dir=in action=allow protocol=UDP localport=1434
		#Write-Host "Allowing multicast broadcast response on UDP (Browser Service Enumerations OK)"
		#netsh firewall set multicastbroadcastresponse ENABLE	
}

function Main(
	[Parameter(Mandatory=$false)]$EnvironmentName,
	[Parameter(Mandatory=$false)]$ServerRole,
    [Parameter(Mandatory=$false)][string]$GlobalConfigurationFileName,
	[Parameter(Mandatory=$false)][String]$XMLDirectory
)
{

	$EnvironmentConfigurationFileName=$null
	$ServerName = $env:computername

    $GlobalConfigurationFileName = Ensure-EnvironmentFile $GlobalConfigurationFileName
    $GlobalConfiguration = Get-GlobalConfiguration -InputFile $GlobalConfigurationFileName

    $EnvironmentName = Ensure-EnvironmentName -EnvironmentName $EnvironmentName -ServerName $ServerName -EnvironmentConfigurationFiles $GlobalConfiguration.EnvironmentConfigurationFiles

    $EnvironmentConfigurationFileName = Ensure-EnvironmentConfigurationFileName $EnvironmentConfigurationFileName $EnvironmentName $ServerName $GlobalConfiguration.EnvironmentConfigurationFiles
    $EnvironmentConfiguration = Get-EnvironmentConfiguration -EnvironmentConfigurationFileName $EnvironmentConfigurationFileName -EnvironmentName $EnvironmentName -XMLDirectory $XMLDirectory

    $ServerRole = Ensure-DeploymentTier -DeploymentTier $ServerRole -ServerName $ServerName -EnvironmentConfiguration $EnvironmentConfiguration
	
	#All Servers
	CreateAllPortsRules
	# Need for Ping and AppFabric / SharePoint Distributed Cache
	EnableInboundFirewallRule "File and Printer Sharing (Echo Request - ICMPv4-In)" "Domain"


	if ($ServerRole -eq "Portal")
	{
		#AppFabric / SharePoint Distributed Cache
		EnableInboundFirewallRule "Remote Service Management (NP-In)" "Domain"
		#AppFabric Caching Service (TCP-In) - Enabled by default
		#AppFabric Caching Service (TCP-Out) - Enabled by default
		
		#DTC Inbound Rules:
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC-EPMAP)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (TCP-In)" "Domain"
	}

	if ($ServerRole -eq "Services")
	{
		#AppFabric / SharePoint Distributed Cache
		EnableInboundFirewallRule "Remote Service Management (NP-In)" "Domain"
		##AppFabric Caching Service (TCP-Out) - Enabled by default
		
		#DTC Inbound Rules:
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC-EPMAP)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (TCP-In)" "Domain"
	}

	if (($ServerRole -eq "SPWFE") -or ($ServerRole -eq "SPWFE"))
	{
		EnableInboundFirewallRule "Remote Service Management (NP-In)" "Domain"
		#Node1 Fabric Process Exception
		#AppFabric Caching Service (TCP-In)
	}

	if ($ServerRole -eq "SQL")
	{
		EnableInboundFirewallRule "Remote Service Management (NP-In)" "Domain"
		
		#DTC Inbound Rules:
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (RPC-EPMAP)" "Domain"
		EnableInboundFirewallRule "Distributed Transaction Coordinator (TCP-In)" "Domain"
			
		Write-Host "=========  SQL Server Ports  ==================="
		Write-Host "Enabling SQLServer default instance port 1433"
		netsh advfirewall firewall add rule name="SQL Server" dir=in action=allow protocol=TCP localport=1433
		
		Write-Host "Enabling Dedicated Admin Connection port 1434"
		netsh advfirewall firewall add rule name="SQL Admin Connection" dir=in action=allow protocol=TCP localport=1434

		#Write-Host "Enabling UL_Aria instance port 1435"
		#netsh advfirewall firewall add rule name="SQL Admin Connection" dir=in action=allow protocol=TCP localport=1435
		
	}
}

Main -ServerRole $ServerRole -EnvironmentName $EnvironmentName -GlobalConfigurationFileName $GlobalConfigurationFileName -XMLDirectory $XMLDirectory