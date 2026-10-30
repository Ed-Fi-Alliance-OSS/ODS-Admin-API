$apiPrefix = "v"
# Install the MinVer CLI tool
&dotnet tool install --global minver-cli
# version
$version = $(minver -t $apiPrefix)
# Fallback to MinVer
$apiVersion = "$version"          

return $apiVersion