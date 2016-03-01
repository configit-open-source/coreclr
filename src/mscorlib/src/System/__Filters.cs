using System.Reflection;

namespace System
{
    internal class __Filters
    {
        internal static readonly __Filters Instance = new __Filters();
        internal virtual bool FilterAttribute(MemberInfo m, Object filterCriteria)
        {
            if (filterCriteria == null)
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
            switch (m.MemberType)
            {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                {
                    MethodAttributes criteria = 0;
                    try
                    {
                        int i = (int)filterCriteria;
                        criteria = (MethodAttributes)i;
                    }
                    catch
                    {
                        throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
                    }

                    MethodAttributes attr;
                    if (m.MemberType == MemberTypes.Method)
                        attr = ((MethodInfo)m).Attributes;
                    else
                        attr = ((ConstructorInfo)m).Attributes;
                    if (((criteria & MethodAttributes.MemberAccessMask) != 0) && (attr & MethodAttributes.MemberAccessMask) != (criteria & MethodAttributes.MemberAccessMask))
                        return false;
                    if (((criteria & MethodAttributes.Static) != 0) && (attr & MethodAttributes.Static) == 0)
                        return false;
                    if (((criteria & MethodAttributes.Final) != 0) && (attr & MethodAttributes.Final) == 0)
                        return false;
                    if (((criteria & MethodAttributes.Virtual) != 0) && (attr & MethodAttributes.Virtual) == 0)
                        return false;
                    if (((criteria & MethodAttributes.Abstract) != 0) && (attr & MethodAttributes.Abstract) == 0)
                        return false;
                    if (((criteria & MethodAttributes.SpecialName) != 0) && (attr & MethodAttributes.SpecialName) == 0)
                        return false;
                    return true;
                }

                case MemberTypes.Field:
                {
                    FieldAttributes criteria = 0;
                    try
                    {
                        int i = (int)filterCriteria;
                        criteria = (FieldAttributes)i;
                    }
                    catch
                    {
                        throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
                    }

                    FieldAttributes attr = ((FieldInfo)m).Attributes;
                    if (((criteria & FieldAttributes.FieldAccessMask) != 0) && (attr & FieldAttributes.FieldAccessMask) != (criteria & FieldAttributes.FieldAccessMask))
                        return false;
                    if (((criteria & FieldAttributes.Static) != 0) && (attr & FieldAttributes.Static) == 0)
                        return false;
                    if (((criteria & FieldAttributes.InitOnly) != 0) && (attr & FieldAttributes.InitOnly) == 0)
                        return false;
                    if (((criteria & FieldAttributes.Literal) != 0) && (attr & FieldAttributes.Literal) == 0)
                        return false;
                    if (((criteria & FieldAttributes.NotSerialized) != 0) && (attr & FieldAttributes.NotSerialized) == 0)
                        return false;
                    if (((criteria & FieldAttributes.PinvokeImpl) != 0) && (attr & FieldAttributes.PinvokeImpl) == 0)
                        return false;
                    return true;
                }
            }

            return false;
        }

        internal virtual bool FilterName(MemberInfo m, Object filterCriteria)
        {
            if (filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritString"));
            String str = ((String)filterCriteria);
            str = str.Trim();
            String name = m.Name;
            if (m.MemberType == MemberTypes.NestedType)
                name = name.Substring(name.LastIndexOf('+') + 1);
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return (name.StartsWith(str, StringComparison.Ordinal));
            }

            return (name.Equals(str));
        }

        internal virtual bool FilterIgnoreCase(MemberInfo m, Object filterCriteria)
        {
            if (filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritString"));
            String str = (String)filterCriteria;
            str = str.Trim();
            String name = m.Name;
            if (m.MemberType == MemberTypes.NestedType)
                name = name.Substring(name.LastIndexOf('+') + 1);
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return (String.Compare(name, 0, str, 0, str.Length, StringComparison.OrdinalIgnoreCase) == 0);
            }

            return (String.Compare(str, name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}