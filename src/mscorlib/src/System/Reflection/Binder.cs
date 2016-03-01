using System.Globalization;

namespace System.Reflection
{
    public abstract class Binder
    {
        public abstract MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref Object[] args, ParameterModifier[] modifiers, CultureInfo culture, String[] names, out Object state);
        public abstract FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, Object value, CultureInfo culture);
        public abstract MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers);
        public abstract PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers);
        public abstract Object ChangeType(Object value, Type type, CultureInfo culture);
        public abstract void ReorderArgumentArray(ref Object[] args, Object state);
    }
}