dotnet restore RabbitMQ.Toolbox.sln
dotnet build --no-restore src/RabbitMQ.CLI/RabbitMQ.CLI.csproj
dotnet publish --no-build --self-contained true -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true src/RabbitMQ.CLI/RabbitMQ.CLI.csproj -o ./publish

$timestamp = Get-Date -Format "yyyy-MM-dd"
$archiveName = "./RabbitMQ-CLI-$timestamp.zip"

Compress-Archive -Path "./publish/rabbitcli.exe" -DestinationPath $archiveName
Compress-Archive -Path "install.ps1" -Update -DestinationPath $archiveName

Remove-Item -Path "./publish" -Recurse

echo "Upload to GoogleDrive: https://drive.google.com/drive/folders/1Iu_kTLrOomZIwmG4-YZszX0IeYsYKl-b?usp=sharing"