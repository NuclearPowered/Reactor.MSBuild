using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace DataMiner
{
    public class Stubber
    {
        public AssemblyDefinition Assembly { get; }

        public ModuleDefinition Module => Assembly.Modules.Single();

        public Stubber(AssemblyDefinition assembly)
        {
            Assembly = assembly;
        }

        public Stubber(string file) : this(AssemblyDefinition.ReadAssembly(file, new ReaderParameters { ReadWrite = true }))
        {
        }

        public void ClearMethodBodies()
        {
            foreach (var method in Module.Types.SelectMany(x => x.Methods))
            {
                if (method.HasBody)
                {
                    var il = method.Body.GetILProcessor();
                    il.Clear();
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Throw);
                }
            }
        }

        private void RemoveNonPublic<T>(Collection<T> collection) where T : IMemberDefinition
        {
            foreach (var memberDefinition in collection.ToArray())
            {
                var isPublic = memberDefinition switch
                {
                    EventDefinition eventDefinition => (eventDefinition.AddMethod?.IsPublic ?? false) || (eventDefinition.RemoveMethod?.IsPublic ?? false) || (eventDefinition.InvokeMethod?.IsPublic ?? false),
                    FieldDefinition fieldDefinition => fieldDefinition.IsPublic,
                    MethodDefinition methodDefinition => methodDefinition.IsPublic,
                    PropertyDefinition propertyDefinition => (propertyDefinition.GetMethod?.IsPublic ?? false) || (propertyDefinition.SetMethod?.IsPublic ?? false),
                    TypeDefinition typeDefinition => !typeDefinition.IsNotPublic,
                    _ => throw new ArgumentOutOfRangeException(nameof(memberDefinition))
                };

                if (!isPublic)
                {
                    collection.Remove(memberDefinition);
                }
            }
        }

        public void RemoveNonPublic()
        {
            foreach (var type in Module.GetAllTypes().ToArray())
            {
                if (!type.IsNested && type.IsNotPublic)
                {
                    Module.Types.Remove(type);
                    continue;
                }

                RemoveNonPublic(type.Events);
                RemoveNonPublic(type.Fields);
                RemoveNonPublic(type.Methods);
                RemoveNonPublic(type.Properties);
                RemoveNonPublic(type.NestedTypes);
            }
        }

        private void RemoveCustomAttributes(ICustomAttributeProvider provider)
        {
            foreach (var customAttribute in provider.CustomAttributes.ToArray())
            {
                if (customAttribute.AttributeType.Namespace.StartsWith("Unhollower"))
                {
                    provider.CustomAttributes.Remove(customAttribute);
                }
            }
        }

        public void RemoveCustomAttributes()
        {
            foreach (var type in Module.GetAllTypes().ToArray())
            {
                RemoveCustomAttributes(type);

                foreach (var provider in Enumerable.Empty<ICustomAttributeProvider>().Concat(type.Events).Concat(type.Fields).Concat(type.Methods).Concat(type.Properties))
                {
                    RemoveCustomAttributes(provider);
                }
            }
        }

        public void Stub()
        {
            ClearMethodBodies();
            RemoveNonPublic();
            RemoveCustomAttributes();

            Assembly.Write();
            Assembly.Dispose();
        }
    }
}
