using System;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Text.Json.Serialization;

namespace ReflectionDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            BuildClass();
            Console.WriteLine("Hello World!");
        }

        static void BuildClass()
        {
            var properties = GetProperties();
            var typeBuilder = GetTypeBuilder();

            foreach (var item in properties)
            {
                CreateProperty(typeBuilder, item.PropertyName, typeof(string));
            }

            var type = typeBuilder.CreateType();
            var obj = Activator.CreateInstance(type);
            foreach (var item in properties)
            {
                var typeProperties = type.GetProperties();
                var property = typeProperties.SingleOrDefault(t => t.Name == item.PropertyName);
                property.SetValue(obj, item.DefaultValue);
            }
            var assembly = type.Assembly;
            
            var objectJson = JsonSerializer.Serialize(obj);
            Console.WriteLine(objectJson);

        }

        static PropertyScheme[] GetProperties()
        {
            var jsonData = File.ReadAllText("class.json");
            return JsonSerializer.Deserialize<PropertyScheme[]>(jsonData); 
        }

        static TypeBuilder GetTypeBuilder()
        {
            var aName = new AssemblyName("AKSoftware.Languages");
            var aBuilder = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            var moduleBuilder = aBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("LanguageKeys", 
                                                               TypeAttributes.Public | 
                                                               TypeAttributes.Class | 
                                                               TypeAttributes.AutoClass | 
                                                               TypeAttributes.AnsiClass | 
                                                               TypeAttributes.AutoClass | 
                                                               TypeAttributes.BeforeFieldInit, null);




            return typeBuilder;
        }

        static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            var fieldBuilder = typeBuilder.DefineField($"_{propertyName}", propertyType, FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            var getMethodBuilder = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator ilGenerator = getMethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            var setMethodBuilder = typeBuilder.DefineMethod($"set_{propertyName}", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyType });
            ILGenerator setMethodILGenerator = setMethodBuilder.GetILGenerator();
            var modifyProperty = setMethodILGenerator.DefineLabel();
            var exitSet = setMethodILGenerator.DefineLabel();

            setMethodILGenerator.MarkLabel(modifyProperty);
            setMethodILGenerator.Emit(OpCodes.Ldarg_0);
            setMethodILGenerator.Emit(OpCodes.Ldarg_1);
            setMethodILGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            setMethodILGenerator.Emit(OpCodes.Nop);
            setMethodILGenerator.MarkLabel(exitSet);
            setMethodILGenerator.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);

        }
    }

    public class PropertyScheme
    {
        [JsonPropertyName("name")]
        public string PropertyName { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("defaultValue")]
        public string DefaultValue { get; set; }
    }
}
