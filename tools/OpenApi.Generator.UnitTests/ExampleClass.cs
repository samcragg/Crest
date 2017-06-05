//
// Note this file is used to generate the XML for the XmlDocParser tests. Any
// changes will need to set the generate XML option to true in the project
// properties and move the contents to ExampleClass.xml
//
namespace OpenApi.Generator.UnitTests
{
    /// <summary>
    /// Summary for the class.
    /// </summary>
    /// <remarks>
    /// Remarks for the class.
    /// </remarks>
    public sealed class ExampleClass
    {
        /// <summary>
        /// Gets the summary for the property.
        /// </summary>
        public string GetProperty { get; }

        /// <summary>
        /// Gets or sets the summary for the property.
        /// </summary>
        public string GetSetProperty { get; set; }

        /// <summary>
        /// The summary for the property.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Summary for the method.
        /// </summary>
        /// <param name="parameter">Parameter description.</param>
        /// <returns>
        /// Return description for the method.
        /// </returns>
        /// <remarks>
        /// Remarks for the method.
        /// </remarks>
        public string Method(int parameter)
        {
            return null;
        }

        /// <summary>
        /// Summary for the method.
        /// </summary>
        public void MethodWithoutParameter()
        {
        }
    }
}
