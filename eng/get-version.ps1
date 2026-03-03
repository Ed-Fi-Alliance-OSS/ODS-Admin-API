$apiPrefix = "v"

# Get the latest stable v2.x tag (excluding pre-release tags)
$latestV2Tag = git tag -l "v2.*" | 
    Where-Object { $_ -match '^v2\.\d+\.\d+$' } | 
    Sort-Object { [version]($_ -replace '^v', '') } -Descending | 
    Select-Object -First 1
          
if ($latestV2Tag)
{
    # Parse version using regex to extract only major.minor.patch
    if ($latestV2Tag -match '^v(\d+)\.(\d+)\.(\d+)$')
    {
        $major = $matches[1]
        $minor = $matches[2]
        $patch = [int]$matches[3] + 1
                
        # Count commits since the latest tag
        $commitCount = git rev-list "$latestV2Tag..HEAD" --count
                
        # Format as X.X.X-alpha.Y
        $apiVersion = "$apiPrefix$major.$minor.$patch-alpha.0.$commitCount"
    }
    else
    {
        throw "Failed to parse version from tag: $latestV2Tag"
    }
}
else
{
    # Install the MinVer CLI tool
    &dotnet tool install --global minver-cli
    # Fallback to MinVer
    $apiVersion = $(minver -t $apiPrefix)
}          

return $apiVersion