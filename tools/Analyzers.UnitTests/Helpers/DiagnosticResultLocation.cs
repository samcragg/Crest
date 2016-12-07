namespace Analyzers.UnitTests.Helpers
{
    using System;

    /// <summary>
    /// Location where the diagnostic appears, as determined by path, line
    /// number, and column number.
    /// </summary>
    public struct DiagnosticResultLocation
    {
        public DiagnosticResultLocation(int line, int column)
        {
            if (line < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(line), "line must be >= -1");
            }

            if (column < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(column), "column must be >= -1");
            }

            this.Line = line;
            this.Column = column;
        }

        public int Column { get; }

        public int Line { get; }
    }
}
