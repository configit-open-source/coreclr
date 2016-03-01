namespace System.Reflection.Emit
{
    using System;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    public sealed class EventBuilder : _EventBuilder
    {
        private EventBuilder()
        {
        }

        internal EventBuilder(ModuleBuilder mod, String name, EventAttributes attr, TypeBuilder type, EventToken evToken)
        {
            m_name = name;
            m_module = mod;
            m_attributes = attr;
            m_evToken = evToken;
            m_type = type;
        }

        public EventToken GetEventToken()
        {
            return m_evToken;
        }

        private void SetMethodSemantics(MethodBuilder mdBuilder, MethodSemanticsAttributes semantics)
        {
            if (mdBuilder == null)
            {
                throw new ArgumentNullException("mdBuilder");
            }

            Contract.EndContractBlock();
            m_type.ThrowIfCreated();
            TypeBuilder.DefineMethodSemantics(m_module.GetNativeHandle(), m_evToken.Token, semantics, mdBuilder.GetToken().Token);
        }

        public void SetAddOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.AddOn);
        }

        public void SetRemoveOnMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.RemoveOn);
        }

        public void SetRaiseMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Fire);
        }

        public void AddOtherMethod(MethodBuilder mdBuilder)
        {
            SetMethodSemantics(mdBuilder, MethodSemanticsAttributes.Other);
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            m_type.ThrowIfCreated();
            TypeBuilder.DefineCustomAttribute(m_module, m_evToken.Token, m_module.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException("customBuilder");
            }

            Contract.EndContractBlock();
            m_type.ThrowIfCreated();
            customBuilder.CreateCustomAttribute(m_module, m_evToken.Token);
        }

        private String m_name;
        private EventToken m_evToken;
        private ModuleBuilder m_module;
        private EventAttributes m_attributes;
        private TypeBuilder m_type;
    }
}