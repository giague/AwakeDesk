# Define paths
$innoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$issFilePath = "..\InnoSetupProject.iss"

$sourceVersionFile = "version.txt"
$sourceReleaseNotesFile = "release_notes.txt"
$destinationFolder = "C:\publish\installers\AwakeDesk"

# Create destination folder if it does not exist
if (-Not (Test-Path -Path $destinationFolder)) {
    New-Item -ItemType Directory -Path $destinationFolder -Force | Out-Null
    Write-Host "Destination folder created: $destinationFolder"
}

$filesCopied = $false
$appVersion = "1.0"  # Default version if version.txt is missing

# Read version from version.txt if available
if (Test-Path -Path $sourceVersionFile) {
    $appVersion = Get-Content -Path $sourceVersionFile -Raw | Out-String
    $appVersion = $appVersion.Trim()  # Remove extra spaces or line breaks
    Copy-Item -Path $sourceVersionFile -Destination $destinationFolder -Force
    Write-Host "Copied $sourceVersionFile to $destinationFolder (Version: $appVersion)"
    $filesCopied = $true
} else {
    Write-Warning "File $sourceVersionFile not found. Using default version: $appVersion"
}

# Copy release notes file if available
if (Test-Path -Path $sourceReleaseNotesFile) {
    Copy-Item -Path $sourceReleaseNotesFile -Destination $destinationFolder -Force
    Write-Host "Copied $sourceReleaseNotesFile to $destinationFolder"
    $filesCopied = $true
} else {
    Write-Warning "File $sourceReleaseNotesFile not found."
}

# Check if Inno Setup exists
if (Test-Path $innoSetupPath) {
    Write-Host "Inno Setup found. Compiling version $appVersion..."

    # Run Inno Setup compiler with the version parameter
    $arguments = "`"$issFilePath`" /DAppVersion=$appVersion"

    Write-Host "Running: $innoSetupPath $arguments"
    $process = Start-Process -FilePath $innoSetupPath -ArgumentList $arguments -PassThru -Wait

    if ($process.ExitCode -eq 0) {
        Write-Host "Installer successfully created."
    } else {
        Write-Error "Compilation error. Exit code: $($process.ExitCode)"
    }
} else {
    Write-Error "Inno Setup not found at path: $innoSetupPath"
}

# Final message
if ($filesCopied) {
    Write-Host "Operation completed successfully."
} else {
    Write-Error "Some errors occurred during the process."
}
