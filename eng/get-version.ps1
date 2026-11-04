$apiPrefix = "v"
# Install the MinVer CLI tool. Send its output to the host so it is not
# captured as part of this script's return value.
&dotnet tool install --global minver-cli | Out-Host
$version = minver -t $apiPrefix
# Callers expect e.g. "vx.x.x-alpha.0.x".
$apiVersion = "$apiPrefix$version"
return $apiVersion
