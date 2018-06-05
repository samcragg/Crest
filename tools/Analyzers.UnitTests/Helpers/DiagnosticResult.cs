namespace Analyzers.UnitTests.Helpers
{
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[] locations;

        public int Column => this.Locations.Length > 0 ? this.Locations[0].Column : -1;

        public string Id
        {
            get;
            set;
        }

        public int Line => this.Locations.Length > 0 ? this.Locations[0].Line : -1;

        public DiagnosticResultLocation[] Locations
        {
            get => this.locations ?? new DiagnosticResultLocation[0];
            set => this.locations = value;
        }

        public string Message
        {
            get;
            set;
        }

        public DiagnosticSeverity Severity
        {
            get;
            set;
        }
    }
}
