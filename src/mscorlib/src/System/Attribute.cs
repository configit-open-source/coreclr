using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
    public abstract class Attribute : _Attribute
    {
        private static Attribute[] InternalGetCustomAttributes(PropertyInfo element, Type type, bool inherit)
        {
                                                Attribute[] attributes = (Attribute[])element.GetCustomAttributes(type, inherit);
            if (!inherit)
                return attributes;
            Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
            List<Attribute> attributeList = new List<Attribute>();
            CopyToArrayList(attributeList, attributes, types);
            Type[] indexParamTypes = GetIndexParameterTypes(element);
            PropertyInfo baseProp = GetParentDefinition(element, indexParamTypes);
            while (baseProp != null)
            {
                attributes = GetCustomAttributes(baseProp, type, false);
                AddAttributesToList(attributeList, attributes, types);
                baseProp = GetParentDefinition(baseProp, indexParamTypes);
            }

            Array array = CreateAttributeArrayHelper(type, attributeList.Count);
            Array.Copy(attributeList.ToArray(), 0, array, 0, attributeList.Count);
            return (Attribute[])array;
        }

        private static bool InternalIsDefined(PropertyInfo element, Type attributeType, bool inherit)
        {
            if (element.IsDefined(attributeType, inherit))
                return true;
            if (inherit)
            {
                AttributeUsageAttribute usage = InternalGetAttributeUsage(attributeType);
                if (!usage.Inherited)
                    return false;
                Type[] indexParamTypes = GetIndexParameterTypes(element);
                PropertyInfo baseProp = GetParentDefinition(element, indexParamTypes);
                while (baseProp != null)
                {
                    if (baseProp.IsDefined(attributeType, false))
                        return true;
                    baseProp = GetParentDefinition(baseProp, indexParamTypes);
                }
            }

            return false;
        }

        private static PropertyInfo GetParentDefinition(PropertyInfo property, Type[] propertyParameters)
        {
                        MethodInfo propAccessor = property.GetGetMethod(true);
            if (propAccessor == null)
                propAccessor = property.GetSetMethod(true);
            RuntimeMethodInfo rtPropAccessor = propAccessor as RuntimeMethodInfo;
            if (rtPropAccessor != null)
            {
                rtPropAccessor = rtPropAccessor.GetParentDefinition();
                if (rtPropAccessor != null)
                {
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                        return rtPropAccessor.DeclaringType.GetProperty(property.Name, property.PropertyType);
                    return rtPropAccessor.DeclaringType.GetProperty(property.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, property.PropertyType, propertyParameters, null);
                }
            }

            return null;
        }

        private static Attribute[] InternalGetCustomAttributes(EventInfo element, Type type, bool inherit)
        {
                                                Attribute[] attributes = (Attribute[])element.GetCustomAttributes(type, inherit);
            if (inherit)
            {
                Dictionary<Type, AttributeUsageAttribute> types = new Dictionary<Type, AttributeUsageAttribute>(11);
                List<Attribute> attributeList = new List<Attribute>();
                CopyToArrayList(attributeList, attributes, types);
                EventInfo baseEvent = GetParentDefinition(element);
                while (baseEvent != null)
                {
                    attributes = GetCustomAttributes(baseEvent, type, false);
                    AddAttributesToList(attributeList, attributes, types);
                    baseEvent = GetParentDefinition(baseEvent);
                }

                Array array = CreateAttributeArrayHelper(type, attributeList.Count);
                Array.Copy(attributeList.ToArray(), 0, array, 0, attributeList.Count);
                return (Attribute[])array;
            }
            else
                return attributes;
        }

        private static EventInfo GetParentDefinition(EventInfo ev)
        {
                        MethodInfo add = ev.GetAddMethod(true);
            RuntimeMethodInfo rtAdd = add as RuntimeMethodInfo;
            if (rtAdd != null)
            {
                rtAdd = rtAdd.GetParentDefinition();
                if (rtAdd != null)
                    return rtAdd.DeclaringType.GetEvent(ev.Name);
            }

            return null;
        }

        private static bool InternalIsDefined(EventInfo element, Type attributeType, bool inherit)
        {
                        if (element.IsDefined(attributeType, inherit))
                return true;
            if (inherit)
            {
                AttributeUsageAttribute usage = InternalGetAttributeUsage(attributeType);
                if (!usage.Inherited)
                    return false;
                EventInfo baseEvent = GetParentDefinition(element);
                while (baseEvent != null)
                {
                    if (baseEvent.IsDefined(attributeType, false))
                        return true;
                    baseEvent = GetParentDefinition(baseEvent);
                }
            }

            return false;
        }

        private static ParameterInfo GetParentDefinition(ParameterInfo param)
        {
                        RuntimeMethodInfo rtMethod = param.Member as RuntimeMethodInfo;
            if (rtMethod != null)
            {
                rtMethod = rtMethod.GetParentDefinition();
                if (rtMethod != null)
                {
                    ParameterInfo[] parameters = rtMethod.GetParameters();
                    return parameters[param.Position];
                }
            }

            return null;
        }

        private static Attribute[] InternalParamGetCustomAttributes(ParameterInfo param, Type type, bool inherit)
        {
                        List<Type> disAllowMultiple = new List<Type>();
            Object[] objAttr;
            if (type == null)
                type = typeof (Attribute);
            objAttr = param.GetCustomAttributes(type, false);
            for (int i = 0; i < objAttr.Length; i++)
            {
                Type objType = objAttr[i].GetType();
                AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);
                if (attribUsage.AllowMultiple == false)
                    disAllowMultiple.Add(objType);
            }

            Attribute[] ret = null;
            if (objAttr.Length == 0)
                ret = CreateAttributeArrayHelper(type, 0);
            else
                ret = (Attribute[])objAttr;
            if (param.Member.DeclaringType == null)
                return ret;
            if (!inherit)
                return ret;
            ParameterInfo baseParam = GetParentDefinition(param);
            while (baseParam != null)
            {
                objAttr = baseParam.GetCustomAttributes(type, false);
                int count = 0;
                for (int i = 0; i < objAttr.Length; i++)
                {
                    Type objType = objAttr[i].GetType();
                    AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);
                    if ((attribUsage.Inherited) && (disAllowMultiple.Contains(objType) == false))
                    {
                        if (attribUsage.AllowMultiple == false)
                            disAllowMultiple.Add(objType);
                        count++;
                    }
                    else
                        objAttr[i] = null;
                }

                Attribute[] attributes = CreateAttributeArrayHelper(type, count);
                count = 0;
                for (int i = 0; i < objAttr.Length; i++)
                {
                    if (objAttr[i] != null)
                    {
                        attributes[count] = (Attribute)objAttr[i];
                        count++;
                    }
                }

                Attribute[] temp = ret;
                ret = CreateAttributeArrayHelper(type, temp.Length + count);
                Array.Copy(temp, ret, temp.Length);
                int offset = temp.Length;
                for (int i = 0; i < attributes.Length; i++)
                    ret[offset + i] = attributes[i];
                baseParam = GetParentDefinition(baseParam);
            }

            return ret;
        }

        private static bool InternalParamIsDefined(ParameterInfo param, Type type, bool inherit)
        {
                                    if (param.IsDefined(type, false))
                return true;
            if (param.Member.DeclaringType == null || !inherit)
                return false;
            ParameterInfo baseParam = GetParentDefinition(param);
            while (baseParam != null)
            {
                Object[] objAttr = baseParam.GetCustomAttributes(type, false);
                for (int i = 0; i < objAttr.Length; i++)
                {
                    Type objType = objAttr[i].GetType();
                    AttributeUsageAttribute attribUsage = InternalGetAttributeUsage(objType);
                    if ((objAttr[i] is Attribute) && (attribUsage.Inherited))
                        return true;
                }

                baseParam = GetParentDefinition(baseParam);
            }

            return false;
        }

        private static void CopyToArrayList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                attributeList.Add(attributes[i]);
                Type attrType = attributes[i].GetType();
                if (!types.ContainsKey(attrType))
                    types[attrType] = InternalGetAttributeUsage(attrType);
            }
        }

        private static Type[] GetIndexParameterTypes(PropertyInfo element)
        {
            ParameterInfo[] indexParams = element.GetIndexParameters();
            if (indexParams.Length > 0)
            {
                Type[] indexParamTypes = new Type[indexParams.Length];
                for (int i = 0; i < indexParams.Length; i++)
                {
                    indexParamTypes[i] = indexParams[i].ParameterType;
                }

                return indexParamTypes;
            }

            return Array.Empty<Type>();
        }

        private static void AddAttributesToList(List<Attribute> attributeList, Attribute[] attributes, Dictionary<Type, AttributeUsageAttribute> types)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                Type attrType = attributes[i].GetType();
                AttributeUsageAttribute usage = null;
                types.TryGetValue(attrType, out usage);
                if (usage == null)
                {
                    usage = InternalGetAttributeUsage(attrType);
                    types[attrType] = usage;
                    if (usage.Inherited)
                        attributeList.Add(attributes[i]);
                }
                else if (usage.Inherited && usage.AllowMultiple)
                {
                    attributeList.Add(attributes[i]);
                }
            }
        }

        private static AttributeUsageAttribute InternalGetAttributeUsage(Type type)
        {
            Object[] obj = type.GetCustomAttributes(typeof (AttributeUsageAttribute), false);
            if (obj.Length == 1)
                return (AttributeUsageAttribute)obj[0];
            if (obj.Length == 0)
                return AttributeUsageAttribute.Default;
            throw new FormatException(Environment.GetResourceString("Format_AttributeUsage", type));
        }

        private static Attribute[] CreateAttributeArrayHelper(Type elementType, int elementCount)
        {
            return (Attribute[])Array.UnsafeCreateInstance(elementType, elementCount);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, Type type)
        {
            return GetCustomAttributes(element, type, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, Type type, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (type == null)
                throw new ArgumentNullException("type");
            if (!type.IsSubclassOf(typeof (Attribute)) && type != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalGetCustomAttributes((PropertyInfo)element, type, inherit);
                case MemberTypes.Event:
                    return InternalGetCustomAttributes((EventInfo)element, type, inherit);
                default:
                    return element.GetCustomAttributes(type, inherit) as Attribute[];
            }
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(MemberInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
                        switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalGetCustomAttributes((PropertyInfo)element, typeof (Attribute), inherit);
                case MemberTypes.Event:
                    return InternalGetCustomAttributes((EventInfo)element, typeof (Attribute), inherit);
                default:
                    return element.GetCustomAttributes(typeof (Attribute), inherit) as Attribute[];
            }
        }

        public static bool IsDefined(MemberInfo element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(MemberInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        switch (element.MemberType)
            {
                case MemberTypes.Property:
                    return InternalIsDefined((PropertyInfo)element, attributeType, inherit);
                case MemberTypes.Event:
                    return InternalIsDefined((EventInfo)element, attributeType, inherit);
                default:
                    return element.IsDefined(attributeType, inherit);
            }
        }

        public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute GetCustomAttribute(MemberInfo element, Type attributeType, bool inherit)
        {
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);
            if (attrib == null || attrib.Length == 0)
                return null;
            if (attrib.Length == 1)
                return attrib[0];
            throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType)
        {
            return (Attribute[])GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
            if (element.Member == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParameterInfo"), "element");
                        MemberInfo member = element.Member;
            if (member.MemberType == MemberTypes.Method && inherit)
                return InternalParamGetCustomAttributes(element, attributeType, inherit) as Attribute[];
            return element.GetCustomAttributes(attributeType, inherit) as Attribute[];
        }

        public static Attribute[] GetCustomAttributes(ParameterInfo element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (element.Member == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParameterInfo"), "element");
                        MemberInfo member = element.Member;
            if (member.MemberType == MemberTypes.Method && inherit)
                return InternalParamGetCustomAttributes(element, null, inherit) as Attribute[];
            return element.GetCustomAttributes(typeof (Attribute), inherit) as Attribute[];
        }

        public static bool IsDefined(ParameterInfo element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(ParameterInfo element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        MemberInfo member = element.Member;
            switch (member.MemberType)
            {
                case MemberTypes.Method:
                    return InternalParamIsDefined(element, attributeType, inherit);
                case MemberTypes.Constructor:
                    return element.IsDefined(attributeType, false);
                case MemberTypes.Property:
                    return element.IsDefined(attributeType, false);
                default:
                                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidParamInfo"));
            }
        }

        public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute GetCustomAttribute(ParameterInfo element, Type attributeType, bool inherit)
        {
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);
            if (attrib == null || attrib.Length == 0)
                return null;
            if (attrib.Length == 0)
                return null;
            if (attrib.Length == 1)
                return attrib[0];
            throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
        }

        public static Attribute[] GetCustomAttributes(Module element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(Module element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(Module element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
                        return (Attribute[])element.GetCustomAttributes(typeof (Attribute), inherit);
        }

        public static Attribute[] GetCustomAttributes(Module element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
        }

        public static bool IsDefined(Module element, Type attributeType)
        {
            return IsDefined(element, attributeType, false);
        }

        public static bool IsDefined(Module element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        return element.IsDefined(attributeType, false);
        }

        public static Attribute GetCustomAttribute(Module element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute GetCustomAttribute(Module element, Type attributeType, bool inherit)
        {
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);
            if (attrib == null || attrib.Length == 0)
                return null;
            if (attrib.Length == 1)
                return attrib[0];
            throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
        }

        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType)
        {
            return GetCustomAttributes(element, attributeType, true);
        }

        public static Attribute[] GetCustomAttributes(Assembly element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        return (Attribute[])element.GetCustomAttributes(attributeType, inherit);
        }

        public static Attribute[] GetCustomAttributes(Assembly element)
        {
            return GetCustomAttributes(element, true);
        }

        public static Attribute[] GetCustomAttributes(Assembly element, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
                        return (Attribute[])element.GetCustomAttributes(typeof (Attribute), inherit);
        }

        public static bool IsDefined(Assembly element, Type attributeType)
        {
            return IsDefined(element, attributeType, true);
        }

        public static bool IsDefined(Assembly element, Type attributeType, bool inherit)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            if (!attributeType.IsSubclassOf(typeof (Attribute)) && attributeType != typeof (Attribute))
                throw new ArgumentException(Environment.GetResourceString("Argument_MustHaveAttributeBaseClass"));
                        return element.IsDefined(attributeType, false);
        }

        public static Attribute GetCustomAttribute(Assembly element, Type attributeType)
        {
            return GetCustomAttribute(element, attributeType, true);
        }

        public static Attribute GetCustomAttribute(Assembly element, Type attributeType, bool inherit)
        {
            Attribute[] attrib = GetCustomAttributes(element, attributeType, inherit);
            if (attrib == null || attrib.Length == 0)
                return null;
            if (attrib.Length == 1)
                return attrib[0];
            throw new AmbiguousMatchException(Environment.GetResourceString("RFLCT.AmbigCust"));
        }

        protected Attribute()
        {
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;
            RuntimeType thisType = (RuntimeType)this.GetType();
            RuntimeType thatType = (RuntimeType)obj.GetType();
            if (thatType != thisType)
                return false;
            Object thisObj = this;
            Object thisResult, thatResult;
            FieldInfo[] thisFields = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < thisFields.Length; i++)
            {
                thisResult = ((RtFieldInfo)thisFields[i]).UnsafeGetValue(thisObj);
                thatResult = ((RtFieldInfo)thisFields[i]).UnsafeGetValue(obj);
                if (!AreFieldValuesEqual(thisResult, thatResult))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreFieldValuesEqual(Object thisValue, Object thatValue)
        {
            if (thisValue == null && thatValue == null)
                return true;
            if (thisValue == null || thatValue == null)
                return false;
            if (thisValue.GetType().IsArray)
            {
                if (!thisValue.GetType().Equals(thatValue.GetType()))
                {
                    return false;
                }

                Array thisValueArray = thisValue as Array;
                Array thatValueArray = thatValue as Array;
                if (thisValueArray.Length != thatValueArray.Length)
                {
                    return false;
                }

                                for (int j = 0; j < thisValueArray.Length; j++)
                {
                    if (!AreFieldValuesEqual(thisValueArray.GetValue(j), thatValueArray.GetValue(j)))
                    {
                        return false;
                    }
                }
            }
            else
            {
                                if (!thisValue.Equals(thatValue))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            Type type = GetType();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Object vThis = null;
            for (int i = 0; i < fields.Length; i++)
            {
                Object fieldValue = ((RtFieldInfo)fields[i]).UnsafeGetValue(this);
                if (fieldValue != null && !fieldValue.GetType().IsArray)
                    vThis = fieldValue;
                if (vThis != null)
                    break;
            }

            if (vThis != null)
                return vThis.GetHashCode();
            return type.GetHashCode();
        }

        public virtual Object TypeId
        {
            get
            {
                return GetType();
            }
        }

        public virtual bool Match(Object obj)
        {
            return Equals(obj);
        }

        public virtual bool IsDefaultAttribute()
        {
            return false;
        }
    }
}