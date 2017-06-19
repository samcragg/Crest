$scriptDirectory = [IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Path)
$source = [IO.Path]::Combine($scriptDirectory, $args[0])
$sourceBytes = [IO.File]::ReadAllBytes($source)

$gzipStream = New-Object IO.Compression.GZipStream ([IO.File]::Create($source + ".gz")), ([IO.Compression.CompressionLevel]::Optimal)
$gzipStream.Write($sourceBytes, 0, $sourceBytes.Length)
$gzipStream.Dispose()