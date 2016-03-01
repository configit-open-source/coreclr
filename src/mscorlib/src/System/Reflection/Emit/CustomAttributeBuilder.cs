
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Emit
{
    public class CustomAttributeBuilder : _CustomAttributeBuilder
    {
        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs)
        {
            InitCustomAttributeBuilder(con, constructorArgs, new PropertyInfo[]{}, new Object[]{}, new FieldInfo[]{}, new Object[]{});
        }

        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs, PropertyInfo[] namedProperties, Object[] propertyValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues, new FieldInfo[]{}, new Object[]{});
        }

        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs, FieldInfo[] namedFields, Object[] fieldValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, new PropertyInfo[]{}, new Object[]{}, namedFields, fieldValues);
        }

        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs, PropertyInfo[] namedProperties, Object[] propertyValues, FieldInfo[] namedFields, Object[] fieldValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);
        }

        private bool ValidateType(Type t)
        {
            if (t.IsPrimitive || t == typeof (String) || t == typeof (Type))
                return true;
            if (t.IsEnum)
            {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(t)))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return true;
                    default:
                        return false;
                }
            }

            if (t.IsArray)
            {
                if (t.GetArrayRank() != 1)
                    return false;
                return ValidateType(t.GetElementType());
            }

            return t == typeof (Object);
        }

        internal void InitCustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs, PropertyInfo[] namedProperties, Object[] propertyValues, FieldInfo[] namedFields, Object[] fieldValues)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (constructorArgs == null)
                throw new ArgumentNullException("constructorArgs");
            if (namedProperties == null)
                throw new ArgumentNullException("namedProperties");
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues");
            if (namedFields == null)
                throw new ArgumentNullException("namedFields");
            if (fieldValues == null)
                throw new ArgumentNullException("fieldValues");
            if (namedProperties.Length != propertyValues.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"), "namedProperties, propertyValues");
            if (namedFields.Length != fieldValues.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"), "namedFields, fieldValues");
                        if ((con.Attributes & MethodAttributes.Static) == MethodAttributes.Static || (con.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadConstructor"));
            if ((con.CallingConvention & CallingConventions.Standard) != CallingConventions.Standard)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadConstructorCallConv"));
            m_con = con;
            m_constructorArgs = new Object[constructorArgs.Length];
            Array.Copy(constructorArgs, m_constructorArgs, constructorArgs.Length);
            Type[] paramTypes;
            int i;
            paramTypes = con.GetParameterTypes();
            if (paramTypes.Length != constructorArgs.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterCountsForConstructor"));
            for (i = 0; i < paramTypes.Length; i++)
                if (!ValidateType(paramTypes[i]))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));
            for (i = 0; i < paramTypes.Length; i++)
            {
                if (constructorArgs[i] == null)
                    continue;
                TypeCode paramTC = Type.GetTypeCode(paramTypes[i]);
                if (paramTC != Type.GetTypeCode(constructorArgs[i].GetType()))
                    if (paramTC != TypeCode.Object || !ValidateType(constructorArgs[i].GetType()))
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForConstructor", i));
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            writer.Write((ushort)1);
            for (i = 0; i < constructorArgs.Length; i++)
                EmitValue(writer, paramTypes[i], constructorArgs[i]);
            writer.Write((ushort)(namedProperties.Length + namedFields.Length));
            for (i = 0; i < namedProperties.Length; i++)
            {
                if (namedProperties[i] == null)
                    throw new ArgumentNullException("namedProperties[" + i + "]");
                Type propType = namedProperties[i].PropertyType;
                if (propertyValues[i] == null && propType.IsPrimitive)
                    throw new ArgumentNullException("propertyValues[" + i + "]");
                if (!ValidateType(propType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));
                if (!namedProperties[i].CanWrite)
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotAWritableProperty"));
                if (namedProperties[i].DeclaringType != con.DeclaringType && (!(con.DeclaringType is TypeBuilderInstantiation)) && !con.DeclaringType.IsSubclassOf(namedProperties[i].DeclaringType))
                {
                    if (!TypeBuilder.IsTypeEqual(namedProperties[i].DeclaringType, con.DeclaringType))
                    {
                        if (!(namedProperties[i].DeclaringType is TypeBuilder) || !con.DeclaringType.IsSubclassOf(((TypeBuilder)namedProperties[i].DeclaringType).BakedRuntimeType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadPropertyForConstructorBuilder"));
                    }
                }

                if (propertyValues[i] != null && propType != typeof (Object) && Type.GetTypeCode(propertyValues[i].GetType()) != Type.GetTypeCode(propType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                writer.Write((byte)CustomAttributeEncoding.Property);
                EmitType(writer, propType);
                EmitString(writer, namedProperties[i].Name);
                EmitValue(writer, propType, propertyValues[i]);
            }

            for (i = 0; i < namedFields.Length; i++)
            {
                if (namedFields[i] == null)
                    throw new ArgumentNullException("namedFields[" + i + "]");
                Type fldType = namedFields[i].FieldType;
                if (fieldValues[i] == null && fldType.IsPrimitive)
                    throw new ArgumentNullException("fieldValues[" + i + "]");
                if (!ValidateType(fldType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));
                if (namedFields[i].DeclaringType != con.DeclaringType && (!(con.DeclaringType is TypeBuilderInstantiation)) && !con.DeclaringType.IsSubclassOf(namedFields[i].DeclaringType))
                {
                    if (!TypeBuilder.IsTypeEqual(namedFields[i].DeclaringType, con.DeclaringType))
                    {
                        if (!(namedFields[i].DeclaringType is TypeBuilder) || !con.DeclaringType.IsSubclassOf(((TypeBuilder)namedFields[i].DeclaringType).BakedRuntimeType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldForConstructorBuilder"));
                    }
                }

                if (fieldValues[i] != null && fldType != typeof (Object) && Type.GetTypeCode(fieldValues[i].GetType()) != Type.GetTypeCode(fldType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                writer.Write((byte)CustomAttributeEncoding.Field);
                EmitType(writer, fldType);
                EmitString(writer, namedFields[i].Name);
                EmitValue(writer, fldType, fieldValues[i]);
            }

            m_blob = ((MemoryStream)writer.BaseStream).ToArray();
        }

        private void EmitType(BinaryWriter writer, Type type)
        {
            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                        writer.Write((byte)CustomAttributeEncoding.SByte);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)CustomAttributeEncoding.Byte);
                        break;
                    case TypeCode.Char:
                        writer.Write((byte)CustomAttributeEncoding.Char);
                        break;
                    case TypeCode.Boolean:
                        writer.Write((byte)CustomAttributeEncoding.Boolean);
                        break;
                    case TypeCode.Int16:
                        writer.Write((byte)CustomAttributeEncoding.Int16);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((byte)CustomAttributeEncoding.UInt16);
                        break;
                    case TypeCode.Int32:
                        writer.Write((byte)CustomAttributeEncoding.Int32);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((byte)CustomAttributeEncoding.UInt32);
                        break;
                    case TypeCode.Int64:
                        writer.Write((byte)CustomAttributeEncoding.Int64);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((byte)CustomAttributeEncoding.UInt64);
                        break;
                    case TypeCode.Single:
                        writer.Write((byte)CustomAttributeEncoding.Float);
                        break;
                    case TypeCode.Double:
                        writer.Write((byte)CustomAttributeEncoding.Double);
                        break;
                    default:
                                                break;
                }
            }
            else if (type.IsEnum)
            {
                writer.Write((byte)CustomAttributeEncoding.Enum);
                EmitString(writer, type.AssemblyQualifiedName);
            }
            else if (type == typeof (String))
            {
                writer.Write((byte)CustomAttributeEncoding.String);
            }
            else if (type == typeof (Type))
            {
                writer.Write((byte)CustomAttributeEncoding.Type);
            }
            else if (type.IsArray)
            {
                writer.Write((byte)CustomAttributeEncoding.Array);
                EmitType(writer, type.GetElementType());
            }
            else
            {
                writer.Write((byte)CustomAttributeEncoding.Object);
            }
        }

        private void EmitString(BinaryWriter writer, String str)
        {
            byte[] utf8Str = Encoding.UTF8.GetBytes(str);
            uint length = (uint)utf8Str.Length;
            if (length <= 0x7f)
            {
                writer.Write((byte)length);
            }
            else if (length <= 0x3fff)
            {
                writer.Write((byte)((length >> 8) | 0x80));
                writer.Write((byte)(length & 0xff));
            }
            else
            {
                writer.Write((byte)((length >> 24) | 0xc0));
                writer.Write((byte)((length >> 16) & 0xff));
                writer.Write((byte)((length >> 8) & 0xff));
                writer.Write((byte)(length & 0xff));
            }

            writer.Write(utf8Str);
        }

        private void EmitValue(BinaryWriter writer, Type type, Object value)
        {
            if (type.IsEnum)
            {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
                {
                    case TypeCode.SByte:
                        writer.Write((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value);
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value);
                        break;
                    default:
                                                break;
                }
            }
            else if (type == typeof (String))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                    EmitString(writer, (String)value);
            }
            else if (type == typeof (Type))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                {
                    String typeName = TypeNameBuilder.ToString((Type)value, TypeNameBuilder.Format.AssemblyQualifiedName);
                    if (typeName == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForCA", value.GetType()));
                    EmitString(writer, typeName);
                }
            }
            else if (type.IsArray)
            {
                if (value == null)
                    writer.Write((uint)0xffffffff);
                else
                {
                    Array a = (Array)value;
                    Type et = type.GetElementType();
                    writer.Write(a.Length);
                    for (int i = 0; i < a.Length; i++)
                        EmitValue(writer, et, a.GetValue(i));
                }
            }
            else if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                        writer.Write((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value);
                        break;
                    case TypeCode.Char:
                        writer.Write(Convert.ToUInt16((char)value));
                        break;
                    case TypeCode.Boolean:
                        writer.Write((byte)((bool)value ? 1 : 0));
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value);
                        break;
                    case TypeCode.Single:
                        writer.Write((float)value);
                        break;
                    case TypeCode.Double:
                        writer.Write((double)value);
                        break;
                    default:
                                                break;
                }
            }
            else if (type == typeof (object))
            {
                Type ot = value == null ? typeof (String) : value is Type ? typeof (Type) : value.GetType();
                if (ot == typeof (object))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForCAB", ot.ToString()));
                EmitType(writer, ot);
                EmitValue(writer, ot, value);
            }
            else
            {
                string typename = "null";
                if (value != null)
                    typename = value.GetType().ToString();
                throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForCAB", typename));
            }
        }

        internal void CreateCustomAttribute(ModuleBuilder mod, int tkOwner)
        {
            CreateCustomAttribute(mod, tkOwner, mod.GetConstructorToken(m_con).Token, false);
        }

        internal int PrepareCreateCustomAttributeToDisk(ModuleBuilder mod)
        {
            return mod.InternalGetConstructorToken(m_con, true).Token;
        }

        internal void CreateCustomAttribute(ModuleBuilder mod, int tkOwner, int tkAttrib, bool toDisk)
        {
            TypeBuilder.DefineCustomAttribute(mod, tkOwner, tkAttrib, m_blob, toDisk, typeof (System.Diagnostics.DebuggableAttribute) == m_con.DeclaringType);
        }

        internal ConstructorInfo m_con;
        internal Object[] m_constructorArgs;
        internal byte[] m_blob;
    }
}