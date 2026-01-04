---
sidebar_position: 10

---

# Powershell Module

The Fig API is just a REST API so can be called from any client or language. One use case for Fig is to integrate into continuous deployment tools and Powershell is one common language used in these tools. Fig comes with a Powershell module which can be imported and used to import value only settings.

The module can be found in the scripts directory.

An example of how it can be used is shown below.

```powershell
Import-Module .\fig-sdk.psm1

[string]$userName = 'admin'
[string]$userPassword = 'admin'

# Convert to SecureString
[securestring]$secStringPassword = ConvertTo-SecureString $userPassword -AsPlainText -Force
[pscredential]$cred = New-Object System.Management.Automation.PSCredential ($userName, $secStringPassword)

Write-Host "Logging in to Fig"
$token = Get-FigAuthToken -Credential $cred -Uri "https://localhost:7281/" # URI is auto resolved from environment variable if it exists

Write-Host "Importing Data"
Submit-FigValueOnlyImport -token $token -jsonFilePath "C:\FigValueOnlyExport.json" -Uri "https://localhost:7281/"

Write-Host "Import Complete."
```

