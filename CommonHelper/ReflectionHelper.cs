using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CommonBll
{
    public class ReflectionHelper
    {
        public static string GetObjectPropertyValue<T>(T t, string propertyname)
        {
            Type type = typeof(T);

            PropertyInfo property = type.GetProperty(propertyname);

            if (property == null) return string.Empty;

            object o = property.GetValue(t, null);

            if (o == null) return string.Empty;

            return o.ToString();
        }

        public static void SetObjectPropertyValue<T>(T t, string propertyname, string propertyType, object value)
        {
            Type type = typeof(T);

            PropertyInfo property = type.GetProperty(propertyname);

            switch (propertyType)
            {
                case "INT":
                    value = Int32.Parse(value.ToString());
                    break;
                case "DATE":
                    value = DateTime.Parse(value.ToString());
                    break;
                case "BOOL":
                    value = Boolean.Parse(value.ToString());
                    break;
            }

            if (property != null)
            {
                property.SetValue(t, value);
            }
        }
    }
}
