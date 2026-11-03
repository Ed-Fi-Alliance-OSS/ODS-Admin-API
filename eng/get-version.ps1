$apiPrefix = "v"
# Install the MinVer CLI tool. Send its output to the host so it is not
# captured as part of this script's return value.
&dotnet tool install --global minver-cli | Out-Host
# version
$version = $(minver -t $apiPrefix)
# Fallback to MinVer
$apiVersion = "$version"

return $apiVersion
# Dummy change
