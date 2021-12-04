using System;
using System.Reflection;
using System.Reflection.Emit;
using ORM.Core.Models.Exceptions;

namespace ORM.Core.Loading
{
    public class ProxyFactory
    {
        public static Type? CreateProxy(Type type)
        {
            if (type.IsSealed)
            {
                throw new OrmException("Cannot create proxy for a class that is sealed");
            }
            
            // Define a dynamic assembly
            var assembly = Assembly.GetCallingAssembly();
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assembly.GetName(), AssemblyBuilderAccess.Run);

            // Define a dynamic module in this assembly.
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ProxyModule");

            // Create a class that derives from the given type
            var typeBuilder = moduleBuilder.DefineType($"{type.Name}Proxy",TypeAttributes.Public | TypeAttributes.Class, type);

            foreach (var property in type.GetProperties())
            {
                if (property.GetMethod?.IsVirtual ?? false)
                {
                    CreateProxyProperty(typeBuilder, property);
                }
            }
            
            return typeBuilder.CreateType();
        }

        private static void CreateProxyProperty(TypeBuilder typeBuilder, PropertyInfo property)
        {
            // Override property
            var newProperty = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, Type.EmptyTypes);

            const MethodAttributes methodAttributes = MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.SpecialName |
                                                      MethodAttributes.HideBySig;
            
            var getter = typeBuilder.DefineMethod("get_" + property.Name, methodAttributes, property.PropertyType, Type.EmptyTypes);
            var setter = typeBuilder.DefineMethod("set_" + property.Name, methodAttributes, null, new[] { property.PropertyType });

            // Create new lazy backing field
            var lazyType = typeof(Lazy<>).MakeGenericType(property.PropertyType);
            var lazyField = typeBuilder.DefineField($"_lazy{property.Name}", lazyType, FieldAttributes.Private);
            var lazyFieldValueProperty = lazyType.GetProperty("Value")?.GetMethod;
            var lazyConstructor = lazyType.GetConstructor(new[] {property.PropertyType});

            if (lazyFieldValueProperty is null)
            {
                throw new OrmException($"Lazy type {lazyType.Name} does not have a property 'Value'.");
            }

            if (lazyConstructor is null)
            {
                throw new OrmException($"Lazy type {lazyType.Name} does not have a constructor with one parameter of type '{property.PropertyType.Name}'.");
            }

            // Get the IL generators to implement custom getter/setter
            var ilGetter = getter.GetILGenerator();
            var ilSetter = setter.GetILGenerator();

            // Getter
            // Load "this"
            ilGetter.Emit(OpCodes.Ldarg_0);
            // Load the lazy backing field
            ilGetter.Emit(OpCodes.Ldfld, lazyField);
            // Get the value property of the lazy backing field
            ilGetter.Emit(OpCodes.Callvirt, lazyFieldValueProperty);
            ilGetter.Emit(OpCodes.Ret);

            // Setter
            // Load "this"
            ilSetter.Emit(OpCodes.Ldarg_0);
            // Load set value
            ilSetter.Emit(OpCodes.Ldarg_1);
            // Create lazy object
            ilSetter.Emit(OpCodes.Newobj, lazyConstructor);
            // Set the lazy backing field to the created object
            ilSetter.Emit(OpCodes.Stfld, lazyField);
            ilSetter.Emit(OpCodes.Ret);

            newProperty.SetGetMethod(getter);
            newProperty.SetSetMethod(setter);
        }
    }
}