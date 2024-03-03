function Get-FigAuthToken {
    param (
        [Parameter(Mandatory = $true)]
        [System.Management.Automation.PSCredential]$Credential,
        [string]$Uri = $env:FIG_API_URI
    )

    try {
        $Uri = Get-FigUri -Uri $Uri

        #get these from the cred.
        $body = @{
            Username = $Credential.GetNetworkCredential().UserName
            Password = $Credential.GetNetworkCredential().Password
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$Uri/users/authenticate" -Method Post -Body $body -ContentType "application/json" -AllowUnencryptedAuthentication
        return $response.Token
    }
    catch {
        throw "Authentication failed: $_"
    }
}

function Submit-FigValueOnlyImport {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Token,
        [Parameter(Mandatory = $true)]
        [string]$JsonFilePath,
        [string]$Uri = $env:FIG_API_URI
    )

    try {
        $Uri = Get-FigUri -Uri $Uri
        $jsonContent = Get-Content -Path $JsonFilePath -Raw
        $response = Invoke-RestMethod -Uri "$Uri/valueonlydata" -Method Put -Body $jsonContent -ContentType "application/json" -Headers @{ "Authorization" = "Bearer $Token" } -AllowUnencryptedAuthentication
        return $response
    }
    catch {
        throw "Failed to submit import: $_"
    }
}

function Get-FigUri {
    param (
        [Parameter(Mandatory = $true)]
        [string]$Uri
    )

    $splitString = $Uri -split ','
    return $splitString[0].Trim().TrimEnd('/')
}


Export-ModuleMember Get-FigAuthToken
Export-ModuleMember Submit-FigValueOnlyImport

