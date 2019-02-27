# Files

To enable clients to upload files, your endpoint can accept a parameter of type
`IFileData` that will automatically be populated with the uploaded file (if you
want to receive multiple files, then change the parameter to be an array).
Since the file comes from the request body, this normally only makes sense for
a `POST`/`PUT` request and, unless it's the only parameter in the method, must
be marked as `FromBody`. For example, here's an endpoint accepting the uploading
of multiple files:

```C#
public interface IFileProcessor
{
    [Put("/file/{id}")]
    [Version(1)]
    Task SaveFile(int id, [FromBody]IFileData file);
}
```

Uploaded files should be uploaded as a multipart request (this is what browsers
and most HTTP clients do for files anyway). The `IFileData` contains the headers
that were sent with the file and a method to get its content. Note that these
headers are the headers for the file part and not the entire request.
