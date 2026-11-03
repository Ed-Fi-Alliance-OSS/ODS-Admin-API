$apiPrefix = "v"
# Install the MinVer CLI tool. Send its output to the host so it is not
# captured as part of this script's return value.
&dotnet tool install --global minver-cli | Out-Host
# MinVer strips the tag prefix from the version it reports, so add it back.
# Callers expect e.g. "v2.5.1-alpha.0.1".
$version = minver -t $apiPrefix
$apiVersion = "$apiPrefix$version"

return $apiVersion
# Dummy change
