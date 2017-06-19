# Embedded Resources

Wherever possible, embedded resources that will be sent to the client should be
pre-compressed to reduce the executable size and also save work at runtime, as
the majority of requests will support compression.

They should be compressed as GZip, as this is easy to convert to DEFLATE by
stripping the 10 byte header and the 8 byte CRC in the footer.

## Helper script

In the build folder there is a PowerShell script to help with this. Open a
PowerShell command prompt in the build directory and run
`.\compress_as_gzip.ps1` followed by the relative path to the file to compress.
The script will output to a file in the same directory as the source file with
the same name but `.gz` appended to the file.

## Other programs

Alternative programs can be used, however, check they do not add additional
headers. For example, using 7-zip adds the original filename to the archive.
This is problematic as it causes a slight increase in file size but, mainly,
requires that we then parse the GZip headers when converting to DEFLATE. Since
we do not do this (the code always assumes the header is 10 bytes) this will
cause an invalid response to the client.
