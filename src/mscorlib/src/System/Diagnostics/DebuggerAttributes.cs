namespace System.Diagnostics
{
    using System;
    using System.Runtime.InteropServices;
    using System.Diagnostics.Contracts;

    public sealed class DebuggerStepThroughAttribute : Attribute
    {
        public DebuggerStepThroughAttribute()
        {
        }
    }

    public sealed class DebuggerStepperBoundaryAttribute : Attribute
    {
        public DebuggerStepperBoundaryAttribute()
        {
        }
    }

    public sealed class DebuggerHiddenAttribute : Attribute
    {
        public DebuggerHiddenAttribute()
        {
        }
    }

    public sealed class DebuggerNonUserCodeAttribute : Attribute
    {
        public DebuggerNonUserCodeAttribute()
        {
        }
    }

    public sealed class DebuggableAttribute : Attribute
    {
        [Flags]
        public enum DebuggingModes
        {
            None = 0x0,
            Default = 0x1,
            DisableOptimizations = 0x100,
            IgnoreSymbolStoreSequencePoints = 0x2,
            EnableEditAndContinue = 0x4
        }

        private DebuggingModes m_debuggingModes;
        public DebuggableAttribute(bool isJITTrackingEnabled, bool isJITOptimizerDisabled)
        {
            m_debuggingModes = 0;
            if (isJITTrackingEnabled)
            {
                m_debuggingModes |= DebuggingModes.Default;
            }

            if (isJITOptimizerDisabled)
            {
                m_debuggingModes |= DebuggingModes.DisableOptimizations;
            }
        }

        public DebuggableAttribute(DebuggingModes modes)
        {
            m_debuggingModes = modes;
        }

        public bool IsJITTrackingEnabled
        {
            get
            {
                return ((m_debuggingModes & DebuggingModes.Default) != 0);
            }
        }

        public bool IsJITOptimizerDisabled
        {
            get
            {
                return ((m_debuggingModes & DebuggingModes.DisableOptimizations) != 0);
            }
        }

        public DebuggingModes DebuggingFlags
        {
            get
            {
                return m_debuggingModes;
            }
        }
    }

    public enum DebuggerBrowsableState
    {
        Never = 0,
        Collapsed = 2,
        RootHidden = 3
    }

    public sealed class DebuggerBrowsableAttribute : Attribute
    {
        private DebuggerBrowsableState state;
        public DebuggerBrowsableAttribute(DebuggerBrowsableState state)
        {
            if (state < DebuggerBrowsableState.Never || state > DebuggerBrowsableState.RootHidden)
                throw new ArgumentOutOfRangeException("state");
            Contract.EndContractBlock();
            this.state = state;
        }

        public DebuggerBrowsableState State
        {
            get
            {
                return state;
            }
        }
    }

    public sealed class DebuggerTypeProxyAttribute : Attribute
    {
        private string typeName;
        private string targetName;
        private Type target;
        public DebuggerTypeProxyAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            Contract.EndContractBlock();
            this.typeName = type.AssemblyQualifiedName;
        }

        public DebuggerTypeProxyAttribute(string typeName)
        {
            this.typeName = typeName;
        }

        public string ProxyTypeName
        {
            get
            {
                return typeName;
            }
        }

        public Type Target
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Contract.EndContractBlock();
                targetName = value.AssemblyQualifiedName;
                target = value;
            }

            get
            {
                return target;
            }
        }

        public string TargetTypeName
        {
            get
            {
                return targetName;
            }

            set
            {
                targetName = value;
            }
        }
    }

    public sealed class DebuggerDisplayAttribute : Attribute
    {
        private string name;
        private string value;
        private string type;
        private string targetName;
        private Type target;
        public DebuggerDisplayAttribute(string value)
        {
            if (value == null)
            {
                this.value = "";
            }
            else
            {
                this.value = value;
            }

            name = "";
            type = "";
        }

        public string Value
        {
            get
            {
                return this.value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        public Type Target
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Contract.EndContractBlock();
                targetName = value.AssemblyQualifiedName;
                target = value;
            }

            get
            {
                return target;
            }
        }

        public string TargetTypeName
        {
            get
            {
                return targetName;
            }

            set
            {
                targetName = value;
            }
        }
    }

    public sealed class DebuggerVisualizerAttribute : Attribute
    {
        private string visualizerObjectSourceName;
        private string visualizerName;
        private string description;
        private string targetName;
        private Type target;
        public DebuggerVisualizerAttribute(string visualizerTypeName)
        {
            this.visualizerName = visualizerTypeName;
        }

        public DebuggerVisualizerAttribute(string visualizerTypeName, string visualizerObjectSourceTypeName)
        {
            this.visualizerName = visualizerTypeName;
            this.visualizerObjectSourceName = visualizerObjectSourceTypeName;
        }

        public DebuggerVisualizerAttribute(string visualizerTypeName, Type visualizerObjectSource)
        {
            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException("visualizerObjectSource");
            }

            Contract.EndContractBlock();
            this.visualizerName = visualizerTypeName;
            this.visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
        }

        public DebuggerVisualizerAttribute(Type visualizer)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException("visualizer");
            }

            Contract.EndContractBlock();
            this.visualizerName = visualizer.AssemblyQualifiedName;
        }

        public DebuggerVisualizerAttribute(Type visualizer, Type visualizerObjectSource)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException("visualizer");
            }

            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException("visualizerObjectSource");
            }

            Contract.EndContractBlock();
            this.visualizerName = visualizer.AssemblyQualifiedName;
            this.visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
        }

        public DebuggerVisualizerAttribute(Type visualizer, string visualizerObjectSourceTypeName)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException("visualizer");
            }

            Contract.EndContractBlock();
            this.visualizerName = visualizer.AssemblyQualifiedName;
            this.visualizerObjectSourceName = visualizerObjectSourceTypeName;
        }

        public string VisualizerObjectSourceTypeName
        {
            get
            {
                return visualizerObjectSourceName;
            }
        }

        public string VisualizerTypeName
        {
            get
            {
                return visualizerName;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        public Type Target
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                Contract.EndContractBlock();
                targetName = value.AssemblyQualifiedName;
                target = value;
            }

            get
            {
                return target;
            }
        }

        public string TargetTypeName
        {
            set
            {
                targetName = value;
            }

            get
            {
                return targetName;
            }
        }
    }
}