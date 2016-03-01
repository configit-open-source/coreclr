namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;

    internal static class Associates
    {
        [Flags]
        internal enum Attributes
        {
            ComposedOfAllVirtualMethods = 0x1,
            ComposedOfAllPrivateMethods = 0x2,
            ComposedOfNoPublicMembers = 0x4,
            ComposedOfNoStaticMembers = 0x8
        }

        internal static bool IncludeAccessor(MethodInfo associate, bool nonPublic)
        {
            if ((object)associate == null)
                return false;
            if (nonPublic)
                return true;
            if (associate.IsPublic)
                return true;
            return false;
        }

        private static unsafe RuntimeMethodInfo AssignAssociates(int tkMethod, RuntimeType declaredType, RuntimeType reflectedType)
        {
            if (MetadataToken.IsNullToken(tkMethod))
                return null;
            Contract.Assert(declaredType != null);
            Contract.Assert(reflectedType != null);
            bool isInherited = declaredType != reflectedType;
            IntPtr[] genericArgumentHandles = null;
            int genericArgumentCount = 0;
            RuntimeType[] genericArguments = declaredType.GetTypeHandleInternal().GetInstantiationInternal();
            if (genericArguments != null)
            {
                genericArgumentCount = genericArguments.Length;
                genericArgumentHandles = new IntPtr[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArgumentHandles[i] = genericArguments[i].GetTypeHandleInternal().Value;
                }
            }

            RuntimeMethodHandleInternal associateMethodHandle = ModuleHandle.ResolveMethodHandleInternalCore(RuntimeTypeHandle.GetModule(declaredType), tkMethod, genericArgumentHandles, genericArgumentCount, null, 0);
            Contract.Assert(!associateMethodHandle.IsNullHandle(), "Failed to resolve associateRecord methodDef token");
            if (isInherited)
            {
                MethodAttributes methAttr = RuntimeMethodHandle.GetAttributes(associateMethodHandle);
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    if ((methAttr & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                        return null;
                }

                if ((methAttr & MethodAttributes.Virtual) != 0)
                {
                    bool declaringTypeIsClass = (RuntimeTypeHandle.GetAttributes(declaredType) & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
                    if (declaringTypeIsClass)
                    {
                        int slot = RuntimeMethodHandle.GetSlot(associateMethodHandle);
                        associateMethodHandle = RuntimeTypeHandle.GetMethodAt(reflectedType, slot);
                    }
                }
            }

            RuntimeMethodInfo associateMethod = RuntimeType.GetMethodBase(reflectedType, associateMethodHandle) as RuntimeMethodInfo;
            if (associateMethod == null)
                associateMethod = reflectedType.Module.ResolveMethod(tkMethod, null, null) as RuntimeMethodInfo;
            return associateMethod;
        }

        internal static unsafe void AssignAssociates(MetadataImport scope, int mdPropEvent, RuntimeType declaringType, RuntimeType reflectedType, out RuntimeMethodInfo addOn, out RuntimeMethodInfo removeOn, out RuntimeMethodInfo fireOn, out RuntimeMethodInfo getter, out RuntimeMethodInfo setter, out MethodInfo[] other, out bool composedOfAllPrivateMethods, out BindingFlags bindingFlags)
        {
            addOn = removeOn = fireOn = getter = setter = null;
            Attributes attributes = Attributes.ComposedOfAllPrivateMethods | Attributes.ComposedOfAllVirtualMethods | Attributes.ComposedOfNoPublicMembers | Attributes.ComposedOfNoStaticMembers;
            while (RuntimeTypeHandle.IsGenericVariable(reflectedType))
                reflectedType = (RuntimeType)reflectedType.BaseType;
            bool isInherited = declaringType != reflectedType;
            List<MethodInfo> otherList = null;
            MetadataEnumResult associatesData;
            scope.Enum(MetadataTokenType.MethodDef, mdPropEvent, out associatesData);
            int cAssociates = associatesData.Length / 2;
            for (int i = 0; i < cAssociates; i++)
            {
                int methodDefToken = associatesData[i * 2];
                MethodSemanticsAttributes semantics = (MethodSemanticsAttributes)associatesData[i * 2 + 1];
                RuntimeMethodInfo associateMethod = AssignAssociates(methodDefToken, declaringType, reflectedType);
                if (associateMethod == null)
                    continue;
                MethodAttributes methAttr = associateMethod.Attributes;
                bool isPrivate = (methAttr & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
                bool isVirtual = (methAttr & MethodAttributes.Virtual) != 0;
                MethodAttributes visibility = methAttr & MethodAttributes.MemberAccessMask;
                bool isPublic = visibility == MethodAttributes.Public;
                bool isStatic = (methAttr & MethodAttributes.Static) != 0;
                if (isPublic)
                {
                    attributes &= ~Attributes.ComposedOfNoPublicMembers;
                    attributes &= ~Attributes.ComposedOfAllPrivateMethods;
                }
                else if (!isPrivate)
                {
                    attributes &= ~Attributes.ComposedOfAllPrivateMethods;
                }

                if (isStatic)
                    attributes &= ~Attributes.ComposedOfNoStaticMembers;
                if (!isVirtual)
                    attributes &= ~Attributes.ComposedOfAllVirtualMethods;
                if (semantics == MethodSemanticsAttributes.Setter)
                    setter = associateMethod;
                else if (semantics == MethodSemanticsAttributes.Getter)
                    getter = associateMethod;
                else if (semantics == MethodSemanticsAttributes.Fire)
                    fireOn = associateMethod;
                else if (semantics == MethodSemanticsAttributes.AddOn)
                    addOn = associateMethod;
                else if (semantics == MethodSemanticsAttributes.RemoveOn)
                    removeOn = associateMethod;
                else
                {
                    if (otherList == null)
                        otherList = new List<MethodInfo>(cAssociates);
                    otherList.Add(associateMethod);
                }
            }

            bool isPseudoPublic = (attributes & Attributes.ComposedOfNoPublicMembers) == 0;
            bool isPseudoStatic = (attributes & Attributes.ComposedOfNoStaticMembers) == 0;
            bindingFlags = RuntimeType.FilterPreCalculate(isPseudoPublic, isInherited, isPseudoStatic);
            composedOfAllPrivateMethods = (attributes & Attributes.ComposedOfAllPrivateMethods) != 0;
            other = (otherList != null) ? otherList.ToArray() : null;
        }
    }
}