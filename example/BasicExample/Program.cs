namespace BasicExample
{
    using Crest.Host.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;

    /// <summary>
    /// Contains the main entry point for the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Represents the entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder();
            host.UseKestrel()
                .UseCrest()
                .Build()
                .Run();
        }
    }
}
