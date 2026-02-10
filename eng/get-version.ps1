$apiPrefix = "v"

# Provisional workaround for branches that do not have the latest tags
# Check if we have a v2.x tag anywhere in the repo
$latestV2Tag = git tag -l "v2.*" --sort=-v:refname | Select-Object -First 1
          
if ($latestV2Tag)
{
  # Parse version
  $version = $latestV2Tag -replace '^v', ''
  $parts = $version -split '\.'
  $major = $parts[0]
  $minor = $parts[1]
  $patch = [int]$parts[2] + 1
            
  # Count commits since the latest tag
  $commitCount = git rev-list "$latestV2Tag..HEAD" --count
            
  # Format as X.X.X-alpha.Y
  $apiVersion = "$apiPrefix$major.$minor.$patch-alpha.0.$commitCount"
}
else
{
  # Install the MinVer CLI tool
  &dotnet tool install --global minver-cli
  # Fallback to MinVer
  $apiVersion = $(minver -t $apiPrefix)
}          

return $apiVersion