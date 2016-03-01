
using System.Globalization;
using System.Reflection;

namespace System
{
    internal class DefaultBinder : Binder
    {
        public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref Object[] args, ParameterModifier[] modifiers, CultureInfo cultureInfo, String[] names, out Object state)
        {
            if (match == null || match.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyArray"), "match");
                        MethodBase[] candidates = (MethodBase[])match.Clone();
            int i;
            int j;
            state = null;
            int[][] paramOrder = new int[candidates.Length][];
            for (i = 0; i < candidates.Length; i++)
            {
                ParameterInfo[] par = candidates[i].GetParametersNoCopy();
                paramOrder[i] = new int[(par.Length > args.Length) ? par.Length : args.Length];
                if (names == null)
                {
                    for (j = 0; j < args.Length; j++)
                        paramOrder[i][j] = j;
                }
                else
                {
                    if (!CreateParamOrder(paramOrder[i], par, names))
                        candidates[i] = null;
                }
            }

            Type[] paramArrayTypes = new Type[candidates.Length];
            Type[] argTypes = new Type[args.Length];
            for (i = 0; i < args.Length; i++)
            {
                if (args[i] != null)
                {
                    argTypes[i] = args[i].GetType();
                }
            }

            int CurIdx = 0;
            bool defaultValueBinding = ((bindingAttr & BindingFlags.OptionalParamBinding) != 0);
            Type paramArrayType = null;
            for (i = 0; i < candidates.Length; i++)
            {
                paramArrayType = null;
                if (candidates[i] == null)
                    continue;
                ParameterInfo[] par = candidates[i].GetParametersNoCopy();
                if (par.Length == 0)
                {
                    if (args.Length != 0)
                    {
                        if ((candidates[i].CallingConvention & CallingConventions.VarArgs) == 0)
                            continue;
                    }

                    paramOrder[CurIdx] = paramOrder[i];
                    candidates[CurIdx++] = candidates[i];
                    continue;
                }
                else if (par.Length > args.Length)
                {
                    for (j = args.Length; j < par.Length - 1; j++)
                    {
                        if (par[j].DefaultValue == System.DBNull.Value)
                            break;
                    }

                    if (j != par.Length - 1)
                        continue;
                    if (par[j].DefaultValue == System.DBNull.Value)
                    {
                        if (!par[j].ParameterType.IsArray)
                            continue;
                        if (!par[j].IsDefined(typeof (ParamArrayAttribute), true))
                            continue;
                        paramArrayType = par[j].ParameterType.GetElementType();
                    }
                }
                else if (par.Length < args.Length)
                {
                    int lastArgPos = par.Length - 1;
                    if (!par[lastArgPos].ParameterType.IsArray)
                        continue;
                    if (!par[lastArgPos].IsDefined(typeof (ParamArrayAttribute), true))
                        continue;
                    if (paramOrder[i][lastArgPos] != lastArgPos)
                        continue;
                    paramArrayType = par[lastArgPos].ParameterType.GetElementType();
                }
                else
                {
                    int lastArgPos = par.Length - 1;
                    if (par[lastArgPos].ParameterType.IsArray && par[lastArgPos].IsDefined(typeof (ParamArrayAttribute), true) && paramOrder[i][lastArgPos] == lastArgPos)
                    {
                        if (!par[lastArgPos].ParameterType.IsAssignableFrom(argTypes[lastArgPos]))
                            paramArrayType = par[lastArgPos].ParameterType.GetElementType();
                    }
                }

                Type pCls = null;
                int argsToCheck = (paramArrayType != null) ? par.Length - 1 : args.Length;
                for (j = 0; j < argsToCheck; j++)
                {
                    pCls = par[j].ParameterType;
                    if (pCls.IsByRef)
                        pCls = pCls.GetElementType();
                    if (pCls == argTypes[paramOrder[i][j]])
                        continue;
                    if (defaultValueBinding && args[paramOrder[i][j]] == Type.Missing)
                        continue;
                    if (args[paramOrder[i][j]] == null)
                        continue;
                    if (pCls == typeof (Object))
                        continue;
                    if (pCls.IsPrimitive)
                    {
                        if (argTypes[paramOrder[i][j]] == null || !CanConvertPrimitiveObjectToType(args[paramOrder[i][j]], (RuntimeType)pCls))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if (argTypes[paramOrder[i][j]] == null)
                            continue;
                        if (!pCls.IsAssignableFrom(argTypes[paramOrder[i][j]]))
                        {
                            if (argTypes[paramOrder[i][j]].IsCOMObject)
                            {
                                if (pCls.IsInstanceOfType(args[paramOrder[i][j]]))
                                    continue;
                            }

                            break;
                        }
                    }
                }

                if (paramArrayType != null && j == par.Length - 1)
                {
                    for (; j < args.Length; j++)
                    {
                        if (paramArrayType.IsPrimitive)
                        {
                            if (argTypes[j] == null || !CanConvertPrimitiveObjectToType(args[j], (RuntimeType)paramArrayType))
                                break;
                        }
                        else
                        {
                            if (argTypes[j] == null)
                                continue;
                            if (!paramArrayType.IsAssignableFrom(argTypes[j]))
                            {
                                if (argTypes[j].IsCOMObject)
                                {
                                    if (paramArrayType.IsInstanceOfType(args[j]))
                                        continue;
                                }

                                break;
                            }
                        }
                    }
                }

                if (j == args.Length)
                {
                    paramOrder[CurIdx] = paramOrder[i];
                    paramArrayTypes[CurIdx] = paramArrayType;
                    candidates[CurIdx++] = candidates[i];
                }
            }

            if (CurIdx == 0)
                throw new MissingMethodException(Environment.GetResourceString("MissingMember"));
            if (CurIdx == 1)
            {
                if (names != null)
                {
                    state = new BinderState((int[])paramOrder[0].Clone(), args.Length, paramArrayTypes[0] != null);
                    ReorderParams(paramOrder[0], args);
                }

                ParameterInfo[] parms = candidates[0].GetParametersNoCopy();
                if (parms.Length == args.Length)
                {
                    if (paramArrayTypes[0] != null)
                    {
                        Object[] objs = new Object[parms.Length];
                        int lastPos = parms.Length - 1;
                        Array.Copy(args, 0, objs, 0, lastPos);
                        objs[lastPos] = Array.UnsafeCreateInstance(paramArrayTypes[0], 1);
                        ((Array)objs[lastPos]).SetValue(args[lastPos], 0);
                        args = objs;
                    }
                }
                else if (parms.Length > args.Length)
                {
                    Object[] objs = new Object[parms.Length];
                    for (i = 0; i < args.Length; i++)
                        objs[i] = args[i];
                    for (; i < parms.Length - 1; i++)
                        objs[i] = parms[i].DefaultValue;
                    if (paramArrayTypes[0] != null)
                        objs[i] = Array.UnsafeCreateInstance(paramArrayTypes[0], 0);
                    else
                        objs[i] = parms[i].DefaultValue;
                    args = objs;
                }
                else
                {
                    if ((candidates[0].CallingConvention & CallingConventions.VarArgs) == 0)
                    {
                        Object[] objs = new Object[parms.Length];
                        int paramArrayPos = parms.Length - 1;
                        Array.Copy(args, 0, objs, 0, paramArrayPos);
                        objs[paramArrayPos] = Array.UnsafeCreateInstance(paramArrayTypes[0], args.Length - paramArrayPos);
                        Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
                        args = objs;
                    }
                }

                return candidates[0];
            }

            int currentMin = 0;
            bool ambig = false;
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder[currentMin], paramArrayTypes[currentMin], candidates[i], paramOrder[i], paramArrayTypes[i], argTypes, args);
                if (newMin == 0)
                {
                    ambig = true;
                }
                else if (newMin == 2)
                {
                    currentMin = i;
                    ambig = false;
                }
            }

            if (ambig)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
            if (names != null)
            {
                state = new BinderState((int[])paramOrder[currentMin].Clone(), args.Length, paramArrayTypes[currentMin] != null);
                ReorderParams(paramOrder[currentMin], args);
            }

            ParameterInfo[] parameters = candidates[currentMin].GetParametersNoCopy();
            if (parameters.Length == args.Length)
            {
                if (paramArrayTypes[currentMin] != null)
                {
                    Object[] objs = new Object[parameters.Length];
                    int lastPos = parameters.Length - 1;
                    Array.Copy(args, 0, objs, 0, lastPos);
                    objs[lastPos] = Array.UnsafeCreateInstance(paramArrayTypes[currentMin], 1);
                    ((Array)objs[lastPos]).SetValue(args[lastPos], 0);
                    args = objs;
                }
            }
            else if (parameters.Length > args.Length)
            {
                Object[] objs = new Object[parameters.Length];
                for (i = 0; i < args.Length; i++)
                    objs[i] = args[i];
                for (; i < parameters.Length - 1; i++)
                    objs[i] = parameters[i].DefaultValue;
                if (paramArrayTypes[currentMin] != null)
                {
                    objs[i] = Array.UnsafeCreateInstance(paramArrayTypes[currentMin], 0);
                }
                else
                {
                    objs[i] = parameters[i].DefaultValue;
                }

                args = objs;
            }
            else
            {
                if ((candidates[currentMin].CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    Object[] objs = new Object[parameters.Length];
                    int paramArrayPos = parameters.Length - 1;
                    Array.Copy(args, 0, objs, 0, paramArrayPos);
                    objs[paramArrayPos] = Array.UnsafeCreateInstance(paramArrayTypes[currentMin], args.Length - paramArrayPos);
                    Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
                    args = objs;
                }
            }

            return candidates[currentMin];
        }

        public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, Object value, CultureInfo cultureInfo)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }

            int i;
            int CurIdx = 0;
            Type valueType = null;
            FieldInfo[] candidates = (FieldInfo[])match.Clone();
            if ((bindingAttr & BindingFlags.SetField) != 0)
            {
                valueType = value.GetType();
                for (i = 0; i < candidates.Length; i++)
                {
                    Type pCls = candidates[i].FieldType;
                    if (pCls == valueType)
                    {
                        candidates[CurIdx++] = candidates[i];
                        continue;
                    }

                    if (value == Empty.Value)
                    {
                        if (pCls.IsClass)
                        {
                            candidates[CurIdx++] = candidates[i];
                            continue;
                        }
                    }

                    if (pCls == typeof (Object))
                    {
                        candidates[CurIdx++] = candidates[i];
                        continue;
                    }

                    if (pCls.IsPrimitive)
                    {
                        if (CanConvertPrimitiveObjectToType(value, (RuntimeType)pCls))
                        {
                            candidates[CurIdx++] = candidates[i];
                            continue;
                        }
                    }
                    else
                    {
                        if (pCls.IsAssignableFrom(valueType))
                        {
                            candidates[CurIdx++] = candidates[i];
                            continue;
                        }
                    }
                }

                if (CurIdx == 0)
                    throw new MissingFieldException(Environment.GetResourceString("MissingField"));
                if (CurIdx == 1)
                    return candidates[0];
            }

            int currentMin = 0;
            bool ambig = false;
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificField(candidates[currentMin], candidates[i]);
                if (newMin == 0)
                    ambig = true;
                else
                {
                    if (newMin == 2)
                    {
                        currentMin = i;
                        ambig = false;
                    }
                }
            }

            if (ambig)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
            return candidates[currentMin];
        }

        public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            int i;
            int j;
            Type[] realTypes = new Type[types.Length];
            for (i = 0; i < types.Length; i++)
            {
                realTypes[i] = types[i].UnderlyingSystemType;
                if (!(realTypes[i] is RuntimeType))
                    throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "types");
            }

            types = realTypes;
            if (match == null || match.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyArray"), "match");
            MethodBase[] candidates = (MethodBase[])match.Clone();
            int CurIdx = 0;
            for (i = 0; i < candidates.Length; i++)
            {
                ParameterInfo[] par = candidates[i].GetParametersNoCopy();
                if (par.Length != types.Length)
                    continue;
                for (j = 0; j < types.Length; j++)
                {
                    Type pCls = par[j].ParameterType;
                    if (pCls == types[j])
                        continue;
                    if (pCls == typeof (Object))
                        continue;
                    if (pCls.IsPrimitive)
                    {
                        if (!(types[j].UnderlyingSystemType is RuntimeType) || !CanConvertPrimitive((RuntimeType)types[j].UnderlyingSystemType, (RuntimeType)pCls.UnderlyingSystemType))
                            break;
                    }
                    else
                    {
                        if (!pCls.IsAssignableFrom(types[j]))
                            break;
                    }
                }

                if (j == types.Length)
                    candidates[CurIdx++] = candidates[i];
            }

            if (CurIdx == 0)
                return null;
            if (CurIdx == 1)
                return candidates[0];
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[types.Length];
            for (i = 0; i < types.Length; i++)
                paramOrder[i] = i;
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder, null, candidates[i], paramOrder, null, types, null);
                if (newMin == 0)
                    ambig = true;
                else
                {
                    if (newMin == 2)
                    {
                        currentMin = i;
                        ambig = false;
                        currentMin = i;
                    }
                }
            }

            if (ambig)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
            return candidates[currentMin];
        }

        public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
        {
            {
                Exception e;
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    e = new NullReferenceException();
                else
                    e = new ArgumentNullException("indexes");
                throw e;
            }

            if (match == null || match.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyArray"), "match");
                        PropertyInfo[] candidates = (PropertyInfo[])match.Clone();
            int i, j = 0;
            int CurIdx = 0;
            int indexesLength = (indexes != null) ? indexes.Length : 0;
            for (i = 0; i < candidates.Length; i++)
            {
                if (indexes != null)
                {
                    ParameterInfo[] par = candidates[i].GetIndexParameters();
                    if (par.Length != indexesLength)
                        continue;
                    for (j = 0; j < indexesLength; j++)
                    {
                        Type pCls = par[j].ParameterType;
                        if (pCls == indexes[j])
                            continue;
                        if (pCls == typeof (Object))
                            continue;
                        if (pCls.IsPrimitive)
                        {
                            if (!(indexes[j].UnderlyingSystemType is RuntimeType) || !CanConvertPrimitive((RuntimeType)indexes[j].UnderlyingSystemType, (RuntimeType)pCls.UnderlyingSystemType))
                                break;
                        }
                        else
                        {
                            if (!pCls.IsAssignableFrom(indexes[j]))
                                break;
                        }
                    }
                }

                if (j == indexesLength)
                {
                    if (returnType != null)
                    {
                        if (candidates[i].PropertyType.IsPrimitive)
                        {
                            if (!(returnType.UnderlyingSystemType is RuntimeType) || !CanConvertPrimitive((RuntimeType)returnType.UnderlyingSystemType, (RuntimeType)candidates[i].PropertyType.UnderlyingSystemType))
                                continue;
                        }
                        else
                        {
                            if (!candidates[i].PropertyType.IsAssignableFrom(returnType))
                                continue;
                        }
                    }

                    candidates[CurIdx++] = candidates[i];
                }
            }

            if (CurIdx == 0)
                return null;
            if (CurIdx == 1)
                return candidates[0];
            int currentMin = 0;
            bool ambig = false;
            int[] paramOrder = new int[indexesLength];
            for (i = 0; i < indexesLength; i++)
                paramOrder[i] = i;
            for (i = 1; i < CurIdx; i++)
            {
                int newMin = FindMostSpecificType(candidates[currentMin].PropertyType, candidates[i].PropertyType, returnType);
                if (newMin == 0 && indexes != null)
                    newMin = FindMostSpecific(candidates[currentMin].GetIndexParameters(), paramOrder, null, candidates[i].GetIndexParameters(), paramOrder, null, indexes, null);
                if (newMin == 0)
                {
                    newMin = FindMostSpecificProperty(candidates[currentMin], candidates[i]);
                    if (newMin == 0)
                        ambig = true;
                }

                if (newMin == 2)
                {
                    ambig = false;
                    currentMin = i;
                }
            }

            if (ambig)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
            return candidates[currentMin];
        }

        public override Object ChangeType(Object value, Type type, CultureInfo cultureInfo)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ChangeType"));
        }

        public override void ReorderArgumentArray(ref Object[] args, Object state)
        {
            BinderState binderState = (BinderState)state;
            ReorderParams(binderState.m_argsMap, args);
            if (binderState.m_isParamArray)
            {
                int paramArrayPos = args.Length - 1;
                if (args.Length == binderState.m_originalSize)
                    args[paramArrayPos] = ((Object[])args[paramArrayPos])[0];
                else
                {
                    Object[] newArgs = new Object[args.Length];
                    Array.Copy(args, 0, newArgs, 0, paramArrayPos);
                    for (int i = paramArrayPos, j = 0; i < newArgs.Length; i++, j++)
                    {
                        newArgs[i] = ((Object[])args[paramArrayPos])[j];
                    }

                    args = newArgs;
                }
            }
            else
            {
                if (args.Length > binderState.m_originalSize)
                {
                    Object[] newArgs = new Object[binderState.m_originalSize];
                    Array.Copy(args, 0, newArgs, 0, binderState.m_originalSize);
                    args = newArgs;
                }
            }
        }

        public static MethodBase ExactBinding(MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
        {
            if (match == null)
                throw new ArgumentNullException("match");
                        MethodBase[] aExactMatches = new MethodBase[match.Length];
            int cExactMatches = 0;
            for (int i = 0; i < match.Length; i++)
            {
                ParameterInfo[] par = match[i].GetParametersNoCopy();
                if (par.Length == 0)
                {
                    continue;
                }

                int j;
                for (j = 0; j < types.Length; j++)
                {
                    Type pCls = par[j].ParameterType;
                    if (!pCls.Equals(types[j]))
                        break;
                }

                if (j < types.Length)
                    continue;
                aExactMatches[cExactMatches] = match[i];
                cExactMatches++;
            }

            if (cExactMatches == 0)
                return null;
            if (cExactMatches == 1)
                return aExactMatches[0];
            return FindMostDerivedNewSlotMeth(aExactMatches, cExactMatches);
        }

        public static PropertyInfo ExactPropertyBinding(PropertyInfo[] match, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (match == null)
                throw new ArgumentNullException("match");
                        PropertyInfo bestMatch = null;
            int typesLength = (types != null) ? types.Length : 0;
            for (int i = 0; i < match.Length; i++)
            {
                ParameterInfo[] par = match[i].GetIndexParameters();
                int j;
                for (j = 0; j < typesLength; j++)
                {
                    Type pCls = par[j].ParameterType;
                    if (pCls != types[j])
                        break;
                }

                if (j < typesLength)
                    continue;
                if (returnType != null && returnType != match[i].PropertyType)
                    continue;
                if (bestMatch != null)
                    throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                bestMatch = match[i];
            }

            return bestMatch;
        }

        private static int FindMostSpecific(ParameterInfo[] p1, int[] paramOrder1, Type paramArrayType1, ParameterInfo[] p2, int[] paramOrder2, Type paramArrayType2, Type[] types, Object[] args)
        {
            if (paramArrayType1 != null && paramArrayType2 == null)
                return 2;
            if (paramArrayType2 != null && paramArrayType1 == null)
                return 1;
            bool p1Less = false;
            bool p2Less = false;
            for (int i = 0; i < types.Length; i++)
            {
                if (args != null && args[i] == Type.Missing)
                    continue;
                Type c1, c2;
                if (paramArrayType1 != null && paramOrder1[i] >= p1.Length - 1)
                    c1 = paramArrayType1;
                else
                    c1 = p1[paramOrder1[i]].ParameterType;
                if (paramArrayType2 != null && paramOrder2[i] >= p2.Length - 1)
                    c2 = paramArrayType2;
                else
                    c2 = p2[paramOrder2[i]].ParameterType;
                if (c1 == c2)
                    continue;
                switch (FindMostSpecificType(c1, c2, types[i]))
                {
                    case 0:
                        return 0;
                    case 1:
                        p1Less = true;
                        break;
                    case 2:
                        p2Less = true;
                        break;
                }
            }

            if (p1Less == p2Less)
            {
                if (!p1Less && args != null)
                {
                    if (p1.Length > p2.Length)
                    {
                        return 1;
                    }
                    else if (p2.Length > p1.Length)
                    {
                        return 2;
                    }
                }

                return 0;
            }
            else
            {
                return (p1Less == true) ? 1 : 2;
            }
        }

        private static int FindMostSpecificType(Type c1, Type c2, Type t)
        {
            if (c1 == c2)
                return 0;
            if (c1 == t)
                return 1;
            if (c2 == t)
                return 2;
            bool c1FromC2;
            bool c2FromC1;
            if (c1.IsByRef || c2.IsByRef)
            {
                if (c1.IsByRef && c2.IsByRef)
                {
                    c1 = c1.GetElementType();
                    c2 = c2.GetElementType();
                }
                else if (c1.IsByRef)
                {
                    if (c1.GetElementType() == c2)
                        return 2;
                    c1 = c1.GetElementType();
                }
                else
                {
                    if (c2.GetElementType() == c1)
                        return 1;
                    c2 = c2.GetElementType();
                }
            }

            if (c1.IsPrimitive && c2.IsPrimitive)
            {
                c1FromC2 = CanConvertPrimitive((RuntimeType)c2, (RuntimeType)c1);
                c2FromC1 = CanConvertPrimitive((RuntimeType)c1, (RuntimeType)c2);
            }
            else
            {
                c1FromC2 = c1.IsAssignableFrom(c2);
                c2FromC1 = c2.IsAssignableFrom(c1);
            }

            if (c1FromC2 == c2FromC1)
                return 0;
            if (c1FromC2)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        private static int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, Type paramArrayType1, MethodBase m2, int[] paramOrder2, Type paramArrayType2, Type[] types, Object[] args)
        {
            int res = FindMostSpecific(m1.GetParametersNoCopy(), paramOrder1, paramArrayType1, m2.GetParametersNoCopy(), paramOrder2, paramArrayType2, types, args);
            if (res != 0)
                return res;
            if (CompareMethodSigAndName(m1, m2))
            {
                int hierarchyDepth1 = GetHierarchyDepth(m1.DeclaringType);
                int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType);
                if (hierarchyDepth1 == hierarchyDepth2)
                {
                    return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }

            return 0;
        }

        private static int FindMostSpecificField(FieldInfo cur1, FieldInfo cur2)
        {
            if (cur1.Name == cur2.Name)
            {
                int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType);
                int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);
                if (hierarchyDepth1 == hierarchyDepth2)
                {
                                        return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                    return 2;
                else
                    return 1;
            }

            return 0;
        }

        private static int FindMostSpecificProperty(PropertyInfo cur1, PropertyInfo cur2)
        {
            if (cur1.Name == cur2.Name)
            {
                int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType);
                int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);
                if (hierarchyDepth1 == hierarchyDepth2)
                {
                    return 0;
                }
                else if (hierarchyDepth1 < hierarchyDepth2)
                    return 2;
                else
                    return 1;
            }

            return 0;
        }

        internal static bool CompareMethodSigAndName(MethodBase m1, MethodBase m2)
        {
            ParameterInfo[] params1 = m1.GetParametersNoCopy();
            ParameterInfo[] params2 = m2.GetParametersNoCopy();
            if (params1.Length != params2.Length)
                return false;
            int numParams = params1.Length;
            for (int i = 0; i < numParams; i++)
            {
                if (params1[i].ParameterType != params2[i].ParameterType)
                    return false;
            }

            return true;
        }

        internal static int GetHierarchyDepth(Type t)
        {
            int depth = 0;
            Type currentType = t;
            do
            {
                depth++;
                currentType = currentType.BaseType;
            }
            while (currentType != null);
            return depth;
        }

        internal static MethodBase FindMostDerivedNewSlotMeth(MethodBase[] match, int cMatches)
        {
            int deepestHierarchy = 0;
            MethodBase methWithDeepestHierarchy = null;
            for (int i = 0; i < cMatches; i++)
            {
                int currentHierarchyDepth = GetHierarchyDepth(match[i].DeclaringType);
                if (currentHierarchyDepth == deepestHierarchy)
                {
                    throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                }

                if (currentHierarchyDepth > deepestHierarchy)
                {
                    deepestHierarchy = currentHierarchyDepth;
                    methWithDeepestHierarchy = match[i];
                }
            }

            return methWithDeepestHierarchy;
        }

        private static extern bool CanConvertPrimitive(RuntimeType source, RuntimeType target);
        static internal extern bool CanConvertPrimitiveObjectToType(Object source, RuntimeType type);
        private static void ReorderParams(int[] paramOrder, Object[] vars)
        {
            object[] varsCopy = new object[vars.Length];
            for (int i = 0; i < vars.Length; i++)
                varsCopy[i] = vars[i];
            for (int i = 0; i < vars.Length; i++)
                vars[i] = varsCopy[paramOrder[i]];
        }

        private static bool CreateParamOrder(int[] paramOrder, ParameterInfo[] pars, String[] names)
        {
            bool[] used = new bool[pars.Length];
            for (int i = 0; i < pars.Length; i++)
                paramOrder[i] = -1;
            for (int i = 0; i < names.Length; i++)
            {
                int j;
                for (j = 0; j < pars.Length; j++)
                {
                    if (names[i].Equals(pars[j].Name))
                    {
                        paramOrder[j] = i;
                        used[i] = true;
                        break;
                    }
                }

                if (j == pars.Length)
                    return false;
            }

            int pos = 0;
            for (int i = 0; i < pars.Length; i++)
            {
                if (paramOrder[i] == -1)
                {
                    for (; pos < pars.Length; pos++)
                    {
                        if (!used[pos])
                        {
                            paramOrder[i] = pos;
                            pos++;
                            break;
                        }
                    }
                }
            }

            return true;
        }

        internal class BinderState
        {
            internal int[] m_argsMap;
            internal int m_originalSize;
            internal bool m_isParamArray;
            internal BinderState(int[] argsMap, int originalSize, bool isParamArray)
            {
                m_argsMap = argsMap;
                m_originalSize = originalSize;
                m_isParamArray = isParamArray;
            }
        }
    }
}