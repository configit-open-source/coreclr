namespace System.Reflection
{
    using System;
    using System.Globalization;

    internal class __Filters
    {
        public virtual bool FilterTypeName(Type cls, Object filterCriteria)
        {
            if (filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(System.Environment.GetResourceString("RFLCT.FltCritString"));
            String str = (String)filterCriteria;
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return cls.Name.StartsWith(str, StringComparison.Ordinal);
            }

            return cls.Name.Equals(str);
        }

        public virtual bool FilterTypeNameIgnoreCase(Type cls, Object filterCriteria)
        {
            if (filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(System.Environment.GetResourceString("RFLCT.FltCritString"));
            String str = (String)filterCriteria;
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                String name = cls.Name;
                if (name.Length >= str.Length)
                    return (String.Compare(name, 0, str, 0, str.Length, StringComparison.OrdinalIgnoreCase) == 0);
                else
                    return false;
            }

            return (String.Compare(str, cls.Name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}