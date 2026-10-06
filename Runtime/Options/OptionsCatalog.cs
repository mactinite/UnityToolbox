using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace toolbox.Options
{
    /// <summary>Finds the static <see cref="Option"/> fields of catalogue classes (see <see cref="OptionsCatalogAttribute"/>).</summary>
    public static class OptionsCatalog
    {
        /// <summary>The non-null static option fields of <paramref name="type"/>, in declaration order.</summary>
        public static List<Option> Collect(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(field => typeof(Option).IsAssignableFrom(field.FieldType))
                .OrderBy(field => field.MetadataToken);

            var result = new List<Option>();
            foreach (var field in fields)
            {
                if (field.GetValue(null) is Option option)
                    result.Add(option);
            }

            return result;
        }

        /// <summary>Every class marked <see cref="OptionsCatalogAttribute"/> in the loaded assemblies. Reflection-heavy; for editor tooling.</summary>
        public static IEnumerable<Type> FindCatalogTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(type => type != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type.IsDefined(typeof(OptionsCatalogAttribute), false))
                        yield return type;
                }
            }
        }
    }
}
