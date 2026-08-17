---
sidebar_position: 11
---

# PowerShell Module

The Fig API is a REST API, so it can be called from any language. Fig ships a PowerShell module for continuous-deployment scripts: authenticate, import value-only settings, and query custom status properties.

The module lives at [`scripts/fig-sdk.psm1`](https://github.com/mzbrau/fig/blob/main/scripts/fig-sdk.psm1) in the repository (and in Fig release zips). Exported cmdlets:

- `Get-FigAuthToken`
- `Submit-FigValueOnlyImport`
- `Get-FigCustomStatusProperties`

These cmdlets use Fig-managed username/password authentication (`POST /users/authenticate`). They are not for [Keycloak](./security.md#keycloak-authentication-mode) mode.

Windows service install from a GitHub release zip is handled separately by [`scripts/Install-Fig.ps1`](https://github.com/mzbrau/fig/blob/main/scripts/Install-Fig.ps1).

## Authenticate and import value-only settings

```powershell
Import-Module .\fig-sdk.psm1

[string]$userName = 'admin'
[string]$userPassword = 'admin'

[securestring]$secStringPassword = ConvertTo-SecureString $userPassword -AsPlainText -Force
[pscredential]$cred = New-Object System.Management.Automation.PSCredential ($userName, $secStringPassword)

Write-Host "Logging in to Fig"
$token = Get-FigAuthToken -Credential $cred -Uri "https://localhost:7281/" # URI is resolved from FIG_API_URI if set

Write-Host "Importing Data"
Submit-FigValueOnlyImport -token $token -jsonFilePath "C:\FigValueOnlyExport.json" -Uri "https://localhost:7281/"

Write-Host "Import Complete."
```

## Query custom status properties

`Get-FigCustomStatusProperties` calls the lightweight `/statuses/properties` APIs and returns one object per property.

```powershell
Import-Module .\fig-sdk.psm1
$token = Get-FigAuthToken -Credential $cred -Uri "https://localhost:7281/"

# All sessions
Get-FigCustomStatusProperties -Token $token -Uri "https://localhost:7281/"

# One client (optional instance)
Get-FigCustomStatusProperties -Token $token -Uri "https://localhost:7281/" -ClientName "AspNetApi"

Get-FigCustomStatusProperties -Token $token -Uri "https://localhost:7281/" -ClientName "AspNetApi" -Instance "prod"

# Filter to a named property (matches Name or DisplayName)
Get-FigCustomStatusProperties -Token $token -Uri "https://localhost:7281/" -ClientName "AspNetApi" -PropertyName "Usage"
```
