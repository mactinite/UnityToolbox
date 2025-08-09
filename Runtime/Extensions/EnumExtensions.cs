using System;

namespace toolbox.Extensions
{
    public static class EnumExtensions
    {
        public static T ToEnum<T>(this string value, T defaultValue)
            where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            T result;
            return Enum.TryParse<T>(value, true, out result) ? result : defaultValue;
        }

        public static T ToEnum<T>(this string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
    }
}
