Write-Host "Fetching node packages..."
npm install --prefix ./build swagger-ui-dist@3.0.18

# We only need these files
$files = @(
    "./build/node_modules/swagger-ui-dist/swagger-ui.css",
    "./build/node_modules/swagger-ui-dist/swagger-ui-bundle.js",
    "./build/node_modules/swagger-ui-dist/swagger-ui-standalone-preset.js"
)
foreach ($file in $files)
{
    Write-Host "Compessing $file..."
    $sourceBytes = [IO.File]::ReadAllBytes([IO.Path]::Combine($pwd, $file))
    $dest = "./src/Crest.OpenApi/SwaggerUI/" + [IO.Path]::GetFileName($file) + ".gz"
    $dest = [IO.Path]::Combine($pwd, $dest)

    $gzipStream = New-Object IO.Compression.GZipStream ([IO.File]::Create($dest)), ([IO.Compression.CompressionLevel]::Optimal)
    $gzipStream.Write($sourceBytes, 0, $sourceBytes.Length)
    $gzipStream.Dispose()
}