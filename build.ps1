$configuration = "Release"
$runtime = "win-x64"
$selfContained = $true
$publishSingleFile = $true

dotnet publish -c $configuration -r $runtime --self-contained $selfContained -p:PublishSingleFile=$publishSingleFile
