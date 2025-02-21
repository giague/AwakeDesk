param (
    [string]$configFile = "App.config",
    [string]$assemblyInfoFile = "AssemblyInfo.cs",
    [string]$versionFile = "version.txt" 
)

# Function to get the value of a key from an XML configuration file
function Get-ConfigValue {
    param (
        [string]$ConfigFile,
        [string]$Key
    )

    try {
        [xml]$xmlConfig = Get-Content -Path $ConfigFile -ErrorAction Stop
        return $xmlConfig.configuration.appSettings.add | Where-Object {$_.key -eq $Key} | Select-Object -ExpandProperty value
    }
    catch {
        Write-Error "Error reading configuration file: $($_.Exception.Message)"
        return $null
    }
}

# Function to update the AssemblyVersion in a file
function Update-AssemblyVersion {
    param (
        [string]$AssemblyInfoFile,
        [string]$NewVersion
    )

    try {
        # Read the content of the AssemblyInfo.cs file
        $assemblyInfoContent = Get-Content -Path $AssemblyInfoFile -ErrorAction Stop

        # Find the line that contains AssemblyVersion
        $assemblyVersionLine = $assemblyInfoContent | Where-Object { $_ -match '\[assembly: AssemblyVersion\("' }

        if ($assemblyVersionLine) {
            # Create the new line with the updated version
            $newAssemblyVersionLine = $assemblyVersionLine -replace '\(".*?"\)', "(""$NewVersion"")"

            # Replace the old line with the new one
            $assemblyInfoContent = $assemblyInfoContent -replace [regex]::Escape($assemblyVersionLine), $newAssemblyVersionLine

            # Write the updated content to the file
            Set-Content -Path $AssemblyInfoFile -Value $assemblyInfoContent -Encoding UTF8 -ErrorAction Stop

            Write-Host "File '$AssemblyInfoFile' updated with version: $NewVersion"
        } else {
            Write-Warning "AssemblyVersion line not found in file '$AssemblyInfoFile'."
        }
    }
    catch {
        Write-Error "Error updating AssemblyInfo file: $($_.Exception.Message)"
    }
}

# Function to update the version.txt file
function Update-VersionFile {
    param (
        [string]$VersionFile,
        [string]$NewVersion
    )

    try {
        # Verifica se il file esiste
        if (Test-Path -Path $VersionFile) {
            # Scrivi il nuovo valore della versione nel file senza aggiungere un a capo
            $NewVersion | Out-File -FilePath $VersionFile -Encoding UTF8 -NoNewline
            Write-Host "File '$VersionFile' updated with version: $NewVersion"
        } else {
            Write-Warning "Version file '$VersionFile' not found. Skipping update."
        }
    }
    catch {
        Write-Error "Error updating version file: $($_.Exception.Message)"
    }
}

# ---------------------- Main Script ----------------------

# Get the version from the configuration file
$currentVersion = Get-ConfigValue -ConfigFile $configFile -Key "CurrentVersion"

# Check if the version was successfully retrieved
if ($currentVersion) {
    Write-Host "Current version retrieved from file '$configFile': $currentVersion"

    # Update the AssemblyInfo.cs file
    Update-AssemblyVersion -AssemblyInfoFile $assemblyInfoFile -NewVersion $currentVersion
    
    # Update version.txt
    Update-VersionFile -VersionFile $versionFile -NewVersion $currentVersion
} else {
    Write-Error "Unable to retrieve version from file '$configFile'.  The process has stopped."
}