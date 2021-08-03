# ATTENTION: This file is not for direct installation purposes. 
# It's used to pack an archive with a published single-file app 
# and optimize the installation of a release

$programPath = "$env:LOCALAPPDATA\RabbitCLI"
New-Item -Path $programPath -ItemType Directory -Force
Copy-Item -Path "rabbitcli.exe" "$programPath\rabbitcli.exe"

[Environment]::SetEnvironmentVariable(
    "Path",
    [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::Machine) + ";$programPath",
    [EnvironmentVariableTarget]::User)

Write-Host "You may have to restart your console in order to refresh the PATH variable."