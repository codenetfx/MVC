<#
.SYNOPSIS
	Configures the Microsoft Distributed Transaction Coordinator (MSTDC)

.DESCRIPTION
	Updates the registry settings.  Peforms the same function as:
	
	1. Open the Component Services snap-in.
		To open Component Services, click Start. In the search box, type dcomcnfg, and then press ENTER.
	2. Expand the console tree to locate the DTC (for example, Local DTC) for which you want to enable Network MS DTC Access.
	3. On the Action menu, click Properties.
	4. Click the Security tab and make the following changes:
		◦ In Security Settings, select the Network DTC Access check box.
		◦ In Transaction Manager Communication, select the Allow Inbound and Allow Outbound check boxes.

.NOTES
	To allow transactions to be coordinated across the network, Network DTC Access 
	must be enabled on all MS DTC instances that are participating in the transaction. 
	
	The MSDTC is used between by both the Portal and middle tier servers with 
	SQL Server.  This is used when writing certain types of entries in the 
	Aria Logging database.
#>

Set-StrictMode -Version 2

Write-Host "Updating Microsoft Distributed Transaction Coordinator registry settings"

Write-Host "Updating distributed transaction registry settings"

#See http://technet.microsoft.com/en-us/library/cc753620(v=WS.10).aspx for a listing of the network setting values

#Enable inbound transactions
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccess -Value 1
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessTransactions -Value 1
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessInbound -Value 1

#Enable outbound transactions
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccess -Value 1
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessTransactions -Value 1
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessOutbound -Value 1

#Enable no authentication required
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC -Name AllowOnlySecureRpcCalls  -Value 0
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC -Name FallbackToUnsecureRPCIfNecessary -Value 0
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC -Name TurnOffRpcSecurity  -Value 1

#Enable XA transactions
Write-Host "Updating distributed transaction registry to allow XA transactions settings"
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name XaTransactions -Value 1

#Enable SNA LU 6.2 transactions
Write-Host "Updating distributed transaction registry to allow SNA LU 6.2 transactions settings"
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name LuTransactions -Value 1

#Enable allow remote clients
Write-Host "Updating distributed transaction registry to allow remote clients"
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessClients -Value 1

#Enable allow remote administration
Write-Host "Updating distributed transaction registry to allow remote administration"
Set-ItemProperty -Path HKLM:\Software\Microsoft\MSDTC\Security -Name NetworkDtcAccessAdmin -Value 1

#Restart DTC Service
Write-Host "Restarting distributed transaction service"
Restart-Service msdtc

Write-Host "Finished updating Microsoft Distributed Transaction Coordinator registry settings"