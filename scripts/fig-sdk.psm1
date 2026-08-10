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

function Get-FigCustomStatusProperties {
    <#
    .SYNOPSIS
        Queries Fig for custom status properties on connected client run sessions.

    .DESCRIPTION
        Calls GET /statuses/properties or GET /statuses/{clientName}/properties and flattens
        each property into a row. Optionally filter by client, instance, and property name.

    .PARAMETER Token
        Bearer token from Get-FigAuthToken.

    .PARAMETER Uri
        Fig API base URI. Defaults to $env:FIG_API_URI.

    .PARAMETER ClientName
        When set, queries only that client's sessions.

    .PARAMETER Instance
        Optional instance filter (requires ClientName for the client-scoped endpoint).

    .PARAMETER PropertyName
        Optional filter matching property Name or DisplayName (case-insensitive).
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$Token,

        [string]$Uri = $env:FIG_API_URI,

        [string]$ClientName,

        [string]$Instance,

        [string]$PropertyName
    )

    try {
        $Uri = Get-FigUri -Uri $Uri
        $headers = @{ "Authorization" = "Bearer $Token" }

        if ([string]::IsNullOrWhiteSpace($ClientName)) {
            $requestUri = "$Uri/statuses/properties"
        }
        else {
            $encodedClient = [System.Uri]::EscapeDataString($ClientName)
            $requestUri = "$Uri/statuses/$encodedClient/properties"
            if (-not [string]::IsNullOrWhiteSpace($Instance)) {
                $encodedInstance = [System.Uri]::EscapeDataString($Instance)
                $requestUri = "$requestUri?instance=$encodedInstance"
            }
        }

        $sessions = Invoke-RestMethod -Uri $requestUri -Method Get -Headers $headers -AllowUnencryptedAuthentication
        if ($null -eq $sessions) {
            return @()
        }

        if ($sessions -isnot [System.Array]) {
            $sessions = @($sessions)
        }

        $rows = foreach ($session in $sessions) {
            $props = $session.CustomProperties.Properties
            if ($null -eq $props) {
                continue
            }

            if ($props -isnot [System.Array]) {
                $props = @($props)
            }

            foreach ($prop in $props) {
                if (-not [string]::IsNullOrWhiteSpace($PropertyName)) {
                    $nameMatch = $prop.Name -and ($prop.Name -ieq $PropertyName)
                    $displayMatch = $prop.DisplayName -and ($prop.DisplayName -ieq $PropertyName)
                    if (-not ($nameMatch -or $displayMatch)) {
                        continue
                    }
                }

                [pscustomobject]@{
                    ClientName   = $session.ClientName
                    Instance     = $session.Instance
                    RunSessionId = $session.RunSessionId
                    LastSeen     = $session.LastSeen
                    Name         = $prop.Name
                    DisplayName  = $prop.DisplayName
                    ValueType    = $prop.ValueType
                    Value        = $prop.Value
                    TextColor    = $prop.TextColor
                    Highlight    = $prop.Highlight
                    ShowInUi     = $prop.ShowInUi
                    Order        = $prop.Order
                }
            }
        }

        return @($rows)
    }
    catch {
        throw "Failed to get custom status properties: $_"
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
Export-ModuleMember Get-FigCustomStatusProperties
