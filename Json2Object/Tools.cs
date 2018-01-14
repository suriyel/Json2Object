using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Json2Object
{
    public static class Tool
    {
        public static object GetObject(this JObject jo, Type type)
        {
            var instance = Activator.CreateInstance(type);

            foreach (var property in jo.Properties())
            {
                SetObject(instance, property);
            }

            return instance;
        }

        private static BindingFlags m_PrivateFlag = BindingFlags.Instance | BindingFlags.NonPublic;

        private static void SetObject(object o, JProperty property)
        {
            var type = o.GetType();

            FieldInfo info;
            //是否是Field
            if (type.TryGetFieldInfo(property.Name, out info))
            {
                SetMememberValue(
                    (value) => info.SetValue(o, value),
                    info.FieldType,
                    property.Value
                    );
            }

            //是否是Property
            PropertyInfo propertyInfo;
            if (type.TryGetPropertyInfo(property.Name, out propertyInfo))
            {
                SetMememberValue(
                    (value) => propertyInfo.SetValue(o, value),
                    propertyInfo.PropertyType,
                    property.Value
                    );
            }
        }

        private static void SetMememberValue(
            Action<object> settter,
            Type memberType,
            JToken token
            )
        {
            if (IsList(memberType))
            {
                //是集合类
                var subType = memberType.GenericTypeArguments[0];
                var list = Activator.CreateInstance(memberType) as IList;

                foreach (var t in token)
                {
                    SetMememberValue(
                        (value) => list.Add(value),
                        subType,
                        t
                        );
                }

                settter(list);
            }
            else
            {
                //是类类型
                if (memberType.IsClass)
                {
                    var classJs = token as JObject;
                    var o = classJs.GetObject(memberType);
                    settter(o);
                }
                else
                {
                    if (m_ConvertDic.ContainsKey(memberType))
                    {
                        var o = m_ConvertDic[memberType](token);
                        settter(o);
                    }
                }
            }
        }

        private static Dictionary<Type, Func<JToken, object>> m_ConvertDic = new Dictionary<Type, Func<JToken, object>>()
        {
            {  typeof (double), (p) => (double)p},
            {  typeof (int), (p) => (int)p},
            {  typeof (float), (p) => (float)p},
            {  typeof (string), (p) => (string)p},
            {  typeof (DateTime), (p) => (DateTime)p},
        };

        private static bool TryGetFieldInfo(this Type type, string name, out FieldInfo info)
        {
            info = type.GetField(name);
            if (info != null)
            {
                return true;
            }

            info = type.GetField(name, m_PrivateFlag);
            return info != null;
        }

        private static bool TryGetPropertyInfo(this Type type, string name, out PropertyInfo info)
        {
            info = type.GetProperty(name);
            if (info != null)
            {
                return true;
            }

            info = type.GetProperty(name, m_PrivateFlag);
            return info != null;
        }

        private static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetInterface(typeof(IList<>).Name) != null;
        }
    }
}
