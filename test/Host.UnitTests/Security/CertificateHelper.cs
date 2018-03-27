namespace Host.UnitTests.Security
{
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;

    internal static class CertificateHelper
    {
        public static X509Certificate2 GetCertificate(string name)
        {
            byte[] rawData;

            Assembly assembly = typeof(CertificateHelper).GetTypeInfo().Assembly;
            using (Stream stream = assembly.GetManifestResourceStream("Host.UnitTests.TestData." + name))
            {
                rawData = new byte[stream.Length];
                stream.Read(rawData, 0, rawData.Length);
            }

            return new X509Certificate2(rawData);
        }
    }
}
