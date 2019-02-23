namespace Host.UnitTests.TestHelpers
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    [TypeConverter(typeof(CustomTypeConverter))]
    public sealed class ClassWithCustomTypeConverter
    {
        public ClassWithCustomTypeConverter(string value)
        {
            this.Value = value;
        }

        public string Value { get; }

        public class CustomTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return true;
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return new ClassWithCustomTypeConverter((string)value);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return ((ClassWithCustomTypeConverter)value).Value;
            }
        }
    }
}
