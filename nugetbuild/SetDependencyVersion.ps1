param(
    [string]$path,
    [string]$id,
    [string]$version
)

[xml]$data = Get-Content -Path $path
$data.package.metadata.dependencies.dependency | %{
    if ($_.id -eq $id) {
        $_.version = $version
    }
}
$data.Save($path)
