Import-Module .\fig-sdk.psm1

[string]$userName = 'admin'
[string]$userPassword = 'admin'

# Convert to SecureString
[securestring]$secStringPassword = ConvertTo-SecureString $userPassword -AsPlainText -Force
[pscredential]$cred = New-Object System.Management.Automation.PSCredential ($userName, $secStringPassword)

Write-Host "Logging in to Fig"
$token = Get-FigAuthToken -Credential $cred -Uri "https://localhost:7281/"

Write-Host "Importing Data"
Submit-FigValueOnlyImport -token $token -jsonFilePath "C:\Users\u043254\Downloads\FigValueOnlyExport-2024-03-02T20_20_33.json" -Uri "https://localhost:7281/"

Write-Host "Import Complete."