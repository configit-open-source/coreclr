
using System.Reflection;

namespace System.Diagnostics.Tracing
{
    internal unsafe struct PropertyValue
    {
        public struct Scalar
        {
            public Boolean AsBoolean;
            public Byte AsByte;
            public SByte AsSByte;
            public Char AsChar;
            public Int16 AsInt16;
            public UInt16 AsUInt16;
            public Int32 AsInt32;
            public UInt32 AsUInt32;
            public Int64 AsInt64;
            public UInt64 AsUInt64;
            public IntPtr AsIntPtr;
            public UIntPtr AsUIntPtr;
            public Single AsSingle;
            public Double AsDouble;
            public Guid AsGuid;
            public DateTime AsDateTime;
            public DateTimeOffset AsDateTimeOffset;
            public TimeSpan AsTimeSpan;
            public Decimal AsDecimal;
        }

        readonly object _reference;
        readonly Scalar _scalar;
        readonly int _scalarLength;
        private PropertyValue(object value)
        {
            _reference = value;
            _scalar = default (Scalar);
            _scalarLength = 0;
        }

        private PropertyValue(Scalar scalar, int scalarLength)
        {
            _reference = null;
            _scalar = scalar;
            _scalarLength = scalarLength;
        }

        private PropertyValue(Boolean value): this (new Scalar()
        {AsBoolean = value}, sizeof (Boolean))
        {
        }

        private PropertyValue(Byte value): this (new Scalar()
        {AsByte = value}, sizeof (Byte))
        {
        }

        private PropertyValue(SByte value): this (new Scalar()
        {AsSByte = value}, sizeof (SByte))
        {
        }

        private PropertyValue(Char value): this (new Scalar()
        {AsChar = value}, sizeof (Char))
        {
        }

        private PropertyValue(Int16 value): this (new Scalar()
        {AsInt16 = value}, sizeof (Int16))
        {
        }

        private PropertyValue(UInt16 value): this (new Scalar()
        {AsUInt16 = value}, sizeof (UInt16))
        {
        }

        private PropertyValue(Int32 value): this (new Scalar()
        {AsInt32 = value}, sizeof (Int32))
        {
        }

        private PropertyValue(UInt32 value): this (new Scalar()
        {AsUInt32 = value}, sizeof (UInt32))
        {
        }

        private PropertyValue(Int64 value): this (new Scalar()
        {AsInt64 = value}, sizeof (Int64))
        {
        }

        private PropertyValue(UInt64 value): this (new Scalar()
        {AsUInt64 = value}, sizeof (UInt64))
        {
        }

        private PropertyValue(IntPtr value): this (new Scalar()
        {AsIntPtr = value}, sizeof (IntPtr))
        {
        }

        private PropertyValue(UIntPtr value): this (new Scalar()
        {AsUIntPtr = value}, sizeof (UIntPtr))
        {
        }

        private PropertyValue(Single value): this (new Scalar()
        {AsSingle = value}, sizeof (Single))
        {
        }

        private PropertyValue(Double value): this (new Scalar()
        {AsDouble = value}, sizeof (Double))
        {
        }

        private PropertyValue(Guid value): this (new Scalar()
        {AsGuid = value}, sizeof (Guid))
        {
        }

        private PropertyValue(DateTime value): this (new Scalar()
        {AsDateTime = value}, sizeof (DateTime))
        {
        }

        private PropertyValue(DateTimeOffset value): this (new Scalar()
        {AsDateTimeOffset = value}, sizeof (DateTimeOffset))
        {
        }

        private PropertyValue(TimeSpan value): this (new Scalar()
        {AsTimeSpan = value}, sizeof (TimeSpan))
        {
        }

        private PropertyValue(Decimal value): this (new Scalar()
        {AsDecimal = value}, sizeof (Decimal))
        {
        }

        public static Func<object, PropertyValue> GetFactory(Type type)
        {
            if (type == typeof (Boolean))
                return value => new PropertyValue((Boolean)value);
            if (type == typeof (Byte))
                return value => new PropertyValue((Byte)value);
            if (type == typeof (SByte))
                return value => new PropertyValue((SByte)value);
            if (type == typeof (Char))
                return value => new PropertyValue((Char)value);
            if (type == typeof (Int16))
                return value => new PropertyValue((Int16)value);
            if (type == typeof (UInt16))
                return value => new PropertyValue((UInt16)value);
            if (type == typeof (Int32))
                return value => new PropertyValue((Int32)value);
            if (type == typeof (UInt32))
                return value => new PropertyValue((UInt32)value);
            if (type == typeof (Int64))
                return value => new PropertyValue((Int64)value);
            if (type == typeof (UInt64))
                return value => new PropertyValue((UInt64)value);
            if (type == typeof (IntPtr))
                return value => new PropertyValue((IntPtr)value);
            if (type == typeof (UIntPtr))
                return value => new PropertyValue((UIntPtr)value);
            if (type == typeof (Single))
                return value => new PropertyValue((Single)value);
            if (type == typeof (Double))
                return value => new PropertyValue((Double)value);
            if (type == typeof (Guid))
                return value => new PropertyValue((Guid)value);
            if (type == typeof (DateTime))
                return value => new PropertyValue((DateTime)value);
            if (type == typeof (DateTimeOffset))
                return value => new PropertyValue((DateTimeOffset)value);
            if (type == typeof (TimeSpan))
                return value => new PropertyValue((TimeSpan)value);
            if (type == typeof (Decimal))
                return value => new PropertyValue((Decimal)value);
            return value => new PropertyValue(value);
        }

        public object ReferenceValue
        {
            get
            {
                                return _reference;
            }
        }

        public Scalar ScalarValue
        {
            get
            {
                                return _scalar;
            }
        }

        public int ScalarLength
        {
            get
            {
                                return _scalarLength;
            }
        }

        public static Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
        {
            if (property.DeclaringType.GetTypeInfo().IsValueType)
                return GetBoxedValueTypePropertyGetter(property);
            else
                return GetReferenceTypePropertyGetter(property);
        }

        private static Func<PropertyValue, PropertyValue> GetBoxedValueTypePropertyGetter(PropertyInfo property)
        {
            var type = property.PropertyType;
            if (type.GetTypeInfo().IsEnum)
                type = Enum.GetUnderlyingType(type);
            var factory = GetFactory(type);
            return container => factory(property.GetValue(container.ReferenceValue));
        }

        private static Func<PropertyValue, PropertyValue> GetReferenceTypePropertyGetter(PropertyInfo property)
        {
            var helper = (TypeHelper)Activator.CreateInstance(typeof (ReferenceTypeHelper<>).MakeGenericType(property.DeclaringType));
            return helper.GetPropertyGetter(property);
        }

        private abstract class TypeHelper
        {
            public abstract Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property);
            protected Delegate GetGetMethod(PropertyInfo property, Type propertyType)
            {
                return property.GetMethod.CreateDelegate(typeof (Func<, >).MakeGenericType(property.DeclaringType, propertyType));
            }
        }

        private sealed class ReferenceTypeHelper<TContainer> : TypeHelper where TContainer : class
        {
            public override Func<PropertyValue, PropertyValue> GetPropertyGetter(PropertyInfo property)
            {
                var type = property.PropertyType;
                if (!Statics.IsValueType(type))
                {
                    var getter = (Func<TContainer, object>)GetGetMethod(property, type);
                    return container => new PropertyValue(getter((TContainer)container.ReferenceValue));
                }
                else
                {
                    if (type.GetTypeInfo().IsEnum)
                        type = Enum.GetUnderlyingType(type);
                    if (type == typeof (Boolean))
                    {
                        var f = (Func<TContainer, Boolean>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Byte))
                    {
                        var f = (Func<TContainer, Byte>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (SByte))
                    {
                        var f = (Func<TContainer, SByte>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Char))
                    {
                        var f = (Func<TContainer, Char>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Int16))
                    {
                        var f = (Func<TContainer, Int16>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (UInt16))
                    {
                        var f = (Func<TContainer, UInt16>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Int32))
                    {
                        var f = (Func<TContainer, Int32>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (UInt32))
                    {
                        var f = (Func<TContainer, UInt32>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Int64))
                    {
                        var f = (Func<TContainer, Int64>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (UInt64))
                    {
                        var f = (Func<TContainer, UInt64>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (IntPtr))
                    {
                        var f = (Func<TContainer, IntPtr>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (UIntPtr))
                    {
                        var f = (Func<TContainer, UIntPtr>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Single))
                    {
                        var f = (Func<TContainer, Single>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Double))
                    {
                        var f = (Func<TContainer, Double>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Guid))
                    {
                        var f = (Func<TContainer, Guid>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (DateTime))
                    {
                        var f = (Func<TContainer, DateTime>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (DateTimeOffset))
                    {
                        var f = (Func<TContainer, DateTimeOffset>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (TimeSpan))
                    {
                        var f = (Func<TContainer, TimeSpan>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    if (type == typeof (Decimal))
                    {
                        var f = (Func<TContainer, Decimal>)GetGetMethod(property, type);
                        return container => new PropertyValue(f((TContainer)container.ReferenceValue));
                    }

                    return container => new PropertyValue(property.GetValue(container.ReferenceValue));
                }
            }
        }
    }
}