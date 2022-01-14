using System;
using System.Reflection;
using System.Reflection.Emit;
using ORM.Core.Models.Exceptions;

namespace ORM.Core.Loading
{
    /// <summary>
    /// Constructs lazy proxies
    /// </summary>
    public static class LazyProxyFactory
    {
        /// <summary>
        /// Creates a lazy proxy for type T. The proxy overrides all virtual properties and inserts lazy backing fields
        /// to be used instead of the original getters and setters.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateProxy<T>()
        {
            var proxyType = CreateProxyType<T>();
            object? proxy = Activator.CreateInstance(proxyType);

            if (proxy is null)
            {
                throw new OrmException($"Failed to create instance for proxy type {proxyType.Name}");
            }
            
            return (T) proxy;
        }
        
        /// <summary>
        /// Creates a proxy type for type T. The proxy type inherits from T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="OrmException"></exception>
        private static Type CreateProxyType<T>()
        {
            var type = typeof(T);
            
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
            var typeBuilder = moduleBuilder.DefineType($"{type.Name}Proxy", TypeAttributes.Public | TypeAttributes.Class, type);

            // Override all virtual properties
            foreach (var property in type.GetProperties())
            {
                if (property.GetMethod?.IsVirtual ?? false)
                {
                    OverrideWithLazyProperty(typeBuilder, property);
                }
            }
            
            var proxyType = typeBuilder.CreateType();

            if (proxyType is null)
            {
                throw new OrmException($"Failed to create lazy proxy type for type {typeof(T).Name}");
            }
            
            return proxyType;
        }

        /// <summary>
        /// Overrides a property with a proxy property.
        /// The proxy property uses lazy backing fields instead of the built in getters and setters.
        /// </summary>
        /// <param name="typeBuilder"></param>
        /// <param name="property"></param>
        /// <exception cref="OrmException"></exception>
        private static void OverrideWithLazyProperty(TypeBuilder typeBuilder, PropertyInfo property)
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
                throw new OrmException($"Lazy type {lazyType.Name} does not have a constructor tha takes one " +
                                       $"parameter of type '{property.PropertyType.Name}'.");
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