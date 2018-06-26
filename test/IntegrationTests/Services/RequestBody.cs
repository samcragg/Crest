namespace IntegrationTests.Services
{
    using System.Globalization;
    using System.Threading.Tasks;
    using Crest.Core;

    public sealed class RequestBody : IRequestBody
    {
        public Task<string> FileMethod(IFileData file)
        {
            return Task.FromResult(file.Filename);
        }

        public Task<string> PutObject(BodyData data)
        {
            return Task.FromResult(
                string.Format(CultureInfo.InvariantCulture, data.Format, data.Arg1));
        }

        public Task<string> PutString(string body)
        {
            return Task.FromResult(body);
        }

        public class BodyData
        {
            public int Arg1 { get; set; }
            public string Format { get; set; }
        }
    }
}
