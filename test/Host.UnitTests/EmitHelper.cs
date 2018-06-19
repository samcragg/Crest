namespace Host.UnitTests
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    internal static class EmitHelper
    {
        public static TypeBuilder CreateTypeBuilder<TBase>()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynamicAssembly" + Guid.NewGuid().ToString()),
                AssemblyBuilderAccess.RunAndCollect);

            TypeBuilder typeBuilder =
                assemblyBuilder.DefineDynamicModule("Module").DefineType(
                    "TestType",
                    TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                    TypeAttributes.BeforeFieldInit | TypeAttributes.Public,
                    typeof(TBase));

            typeBuilder.DefineDefaultConstructor(
                MethodAttributes.HideBySig |
                MethodAttributes.Public |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName);

            return typeBuilder;
        }
    }
}
