namespace Host.UnitTests.Serialization
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class JumpTableGeneratorTests
    {
        private const string GeneratedMethodName = "Method";
        private readonly JumpTableGenerator generator;
        private readonly TypeBuilder typeBuilder = CreateTypeBuilder();

        private JumpTableGeneratorTests()
        {
            this.generator = new JumpTableGenerator(s => s.GetHashCode());
        }

        private static TypeBuilder CreateTypeBuilder()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(JumpTableGeneratorTests) + "_DynamicAssembly"),
                AssemblyBuilderAccess.RunAndCollect);

            TypeBuilder typeBuilder =
                assemblyBuilder.DefineDynamicModule("Module").DefineType(
                    "TestType",
                    TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
                    TypeAttributes.BeforeFieldInit | TypeAttributes.Public);

            typeBuilder.DefineDefaultConstructor(
                MethodAttributes.HideBySig |
                MethodAttributes.Public |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName);

            return typeBuilder;
        }

        private void AddMapping(string key, string returnValue)
        {
            this.generator.Map(key, il => il.Emit(OpCodes.Ldstr, returnValue));
        }

        private string CreateAndInvokeMethod(string parameter)
        {
            this.CreateMethod();
            object instance = Activator.CreateInstance(
                this.typeBuilder.CreateTypeInfo().AsType());

            return (string)instance.GetType()
                .GetMethod(GeneratedMethodName)
                .Invoke(instance, new[] { parameter });
        }

        private MethodBuilder CreateMethod()
        {
            MethodBuilder method = this.typeBuilder.DefineMethod(
                GeneratedMethodName,
                MethodAttributes.HideBySig | MethodAttributes.Public,
                CallingConventions.HasThis);
            method.SetParameters(typeof(string));
            method.SetReturnType(typeof(string));

            ILGenerator il = method.GetILGenerator();
            this.generator.EndOfTable = il.DefineLabel();
            this.generator.EmitJumpTable(il);
            il.MarkLabel(this.generator.EndOfTable);
            il.Emit(OpCodes.Ret);

            return method;
        }

        public sealed class EmitJumpTable : JumpTableGeneratorTests
        {
            private const string NoMatchText = "No Match";

            public EmitJumpTable()
            {
                this.generator.EmitCondition = (il, key) =>
                {
                    // Compare the passed in parameter to the key (0 = this, 1 = param)
                    il.Emit(OpCodes.Ldstr, key);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(
                        OpCodes.Callvirt,
                        typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string) }));
                };

                this.generator.NoMatch = il => il.Emit(OpCodes.Ldstr, NoMatchText);
            }

            [Fact]
            public void ShouldCallTheNoMatchIfNothingIsMatched()
            {
                this.AddMapping("one", "Match");

                string result = this.CreateAndInvokeMethod("two");

                result.Should().Be(NoMatchText);
            }

            [Fact]
            public void ShouldEmitASwitchTableForLotsOfConditions()
            {
                this.AddMapping("1", "one");
                this.AddMapping("2", "two");
                this.AddMapping("3", "three");
                this.AddMapping("4", "four");
                this.AddMapping("5", "five");
                this.AddMapping("6", "six");

                this.generator.EmitGetHashCode = il =>
                {
                    MethodInfo getHashCode = typeof(string).GetMethod(
                        nameof(string.GetHashCode),
                        Type.EmptyTypes);

                    // Use the passed in parameter (0 = this, 1 = param)
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Callvirt, getHashCode);
                };
                string result = this.CreateAndInvokeMethod("5");

                result.Should().Be("five");
            }

            [Fact]
            public void ShouldEmitMulitpleConditions()
            {
                this.AddMapping("1", "one");
                this.AddMapping("2", "two");

                string result = this.CreateAndInvokeMethod("2");

                result.Should().Be("two");
            }
        }
    }
}
