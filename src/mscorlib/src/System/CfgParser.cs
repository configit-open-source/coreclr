using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System
{
    internal enum ConfigEvents
    {
        StartDocument = 0,
        StartDTD = StartDocument + 1,
        EndDTD = StartDTD + 1,
        StartDTDSubset = EndDTD + 1,
        EndDTDSubset = StartDTDSubset + 1,
        EndProlog = EndDTDSubset + 1,
        StartEntity = EndProlog + 1,
        EndEntity = StartEntity + 1,
        EndDocument = EndEntity + 1,
        DataAvailable = EndDocument + 1,
        LastEvent = DataAvailable
    }

    internal enum ConfigNodeType
    {
        Element = 1,
        Attribute = Element + 1,
        Pi = Attribute + 1,
        XmlDecl = Pi + 1,
        DocType = XmlDecl + 1,
        DTDAttribute = DocType + 1,
        EntityDecl = DTDAttribute + 1,
        ElementDecl = EntityDecl + 1,
        AttlistDecl = ElementDecl + 1,
        Notation = AttlistDecl + 1,
        Group = Notation + 1,
        IncludeSect = Group + 1,
        PCData = IncludeSect + 1,
        CData = PCData + 1,
        IgnoreSect = CData + 1,
        Comment = IgnoreSect + 1,
        EntityRef = Comment + 1,
        Whitespace = EntityRef + 1,
        Name = Whitespace + 1,
        NMToken = Name + 1,
        String = NMToken + 1,
        Peref = String + 1,
        Model = Peref + 1,
        ATTDef = Model + 1,
        ATTType = ATTDef + 1,
        ATTPresence = ATTType + 1,
        DTDSubset = ATTPresence + 1,
        LastNodeType = DTDSubset + 1
    }

    internal enum ConfigNodeSubType
    {
        Version = (int)ConfigNodeType.LastNodeType,
        Encoding = Version + 1,
        Standalone = Encoding + 1,
        NS = Standalone + 1,
        XMLSpace = NS + 1,
        XMLLang = XMLSpace + 1,
        System = XMLLang + 1,
        Public = System + 1,
        NData = Public + 1,
        AtCData = NData + 1,
        AtId = AtCData + 1,
        AtIdref = AtId + 1,
        AtIdrefs = AtIdref + 1,
        AtEntity = AtIdrefs + 1,
        AtEntities = AtEntity + 1,
        AtNmToken = AtEntities + 1,
        AtNmTokens = AtNmToken + 1,
        AtNotation = AtNmTokens + 1,
        AtRequired = AtNotation + 1,
        AtImplied = AtRequired + 1,
        AtFixed = AtImplied + 1,
        PentityDecl = AtFixed + 1,
        Empty = PentityDecl + 1,
        Any = Empty + 1,
        Mixed = Any + 1,
        Sequence = Mixed + 1,
        Choice = Sequence + 1,
        Star = Choice + 1,
        Plus = Star + 1,
        Questionmark = Plus + 1,
        LastSubNodeType = Questionmark + 1
    }

    internal abstract class BaseConfigHandler
    {
        protected Delegate[] eventCallbacks;
        public BaseConfigHandler()
        {
            InitializeCallbacks();
        }

        private void InitializeCallbacks()
        {
            if (eventCallbacks == null)
            {
                eventCallbacks = new Delegate[6];
                eventCallbacks[0] = new NotifyEventCallback(this.NotifyEvent);
                eventCallbacks[1] = new BeginChildrenCallback(this.BeginChildren);
                eventCallbacks[2] = new EndChildrenCallback(this.EndChildren);
                eventCallbacks[3] = new ErrorCallback(this.Error);
                eventCallbacks[4] = new CreateNodeCallback(this.CreateNode);
                eventCallbacks[5] = new CreateAttributeCallback(this.CreateAttribute);
            }
        }

        private delegate void NotifyEventCallback(ConfigEvents nEvent);
        public abstract void NotifyEvent(ConfigEvents nEvent);
        private delegate void BeginChildrenCallback(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        public abstract void BeginChildren(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        private delegate void EndChildrenCallback(int fEmpty, int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        public abstract void EndChildren(int fEmpty, int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        private delegate void ErrorCallback(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        public abstract void Error(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        private delegate void CreateNodeCallback(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        public abstract void CreateNode(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        private delegate void CreateAttributeCallback(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        public abstract void CreateAttribute(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength);
        internal extern void RunParser(String fileName);
    }

    internal class ConfigTreeParser : BaseConfigHandler
    {
        ConfigNode rootNode = null;
        ConfigNode currentNode = null;
        String fileName = null;
        int attributeEntry;
        String key = null;
        String[] treeRootPath = null;
        bool parsing = false;
        int depth = 0;
        int pathDepth = 0;
        int searchDepth = 0;
        bool bNoSearchPath = false;
        String lastProcessed = null;
        bool lastProcessedEndElement;
        internal ConfigNode Parse(String fileName, String configPath)
        {
            return Parse(fileName, configPath, false);
        }

        internal ConfigNode Parse(String fileName, String configPath, bool skipSecurityStuff)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
                        this.fileName = fileName;
            if (configPath[0] == '/')
            {
                treeRootPath = configPath.Substring(1).Split('/');
                pathDepth = treeRootPath.Length - 1;
                bNoSearchPath = false;
            }
            else
            {
                treeRootPath = new String[1];
                treeRootPath[0] = configPath;
                bNoSearchPath = true;
            }

            if (!skipSecurityStuff)
            {
                (new FileIOPermission(FileIOPermissionAccess.Read, System.IO.Path.GetFullPathInternal(fileName))).Demand();
            }

            (new SecurityPermission(SecurityPermissionFlag.UnmanagedCode)).Assert();
            try
            {
                RunParser(fileName);
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (DirectoryNotFoundException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (FileLoadException)
            {
                throw;
            }
            catch (Exception inner)
            {
                String message = GetInvalidSyntaxMessage();
                throw new Exception(message, inner);
            }

            return rootNode;
        }

        public override void NotifyEvent(ConfigEvents nEvent)
        {
            BCLDebug.Trace("REMOTE", "NotifyEvent " + ((Enum)nEvent).ToString() + "\n");
        }

        public override void BeginChildren(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength)
        {
            if (!parsing && (!bNoSearchPath && depth == (searchDepth + 1) && String.Compare(text, treeRootPath[searchDepth], StringComparison.Ordinal) == 0))
            {
                searchDepth++;
            }
        }

        public override void EndChildren(int fEmpty, int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength)
        {
            lastProcessed = text;
            lastProcessedEndElement = true;
            if (parsing)
            {
                if (currentNode == rootNode)
                {
                    parsing = false;
                }

                currentNode = currentNode.Parent;
            }
            else if (nType == ConfigNodeType.Element)
            {
                if (depth == searchDepth && String.Compare(text, treeRootPath[searchDepth - 1], StringComparison.Ordinal) == 0)
                {
                    searchDepth--;
                    depth--;
                }
                else
                    depth--;
            }
        }

        public override void Error(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength)
        {
        }

        public override void CreateNode(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength)
        {
            if (nType == ConfigNodeType.Element)
            {
                lastProcessed = text;
                lastProcessedEndElement = false;
                if (parsing || (bNoSearchPath && String.Compare(text, treeRootPath[0], StringComparison.OrdinalIgnoreCase) == 0) || (depth == searchDepth && searchDepth == pathDepth && String.Compare(text, treeRootPath[pathDepth], StringComparison.OrdinalIgnoreCase) == 0))
                {
                    parsing = true;
                    ConfigNode parentNode = currentNode;
                    currentNode = new ConfigNode(text, parentNode);
                    if (rootNode == null)
                        rootNode = currentNode;
                    else
                        parentNode.AddChild(currentNode);
                }
                else
                    depth++;
            }
            else if (nType == ConfigNodeType.PCData)
            {
                if (currentNode != null)
                {
                    currentNode.Value = text;
                }
            }
        }

        public override void CreateAttribute(int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength)
        {
            if (parsing)
            {
                if (nType == ConfigNodeType.Attribute)
                {
                    attributeEntry = currentNode.AddAttribute(text, "");
                    key = text;
                }
                else if (nType == ConfigNodeType.PCData)
                {
                    currentNode.ReplaceAttribute(attributeEntry, key, text);
                }
                else
                {
                    String message = GetInvalidSyntaxMessage();
                    throw new Exception(message);
                }
            }
        }

        private void Trace(String name, int size, ConfigNodeSubType subType, ConfigNodeType nType, int terminal, [MarshalAs(UnmanagedType.LPWStr)] String text, int textLength, int prefixLength, int fEmpty)
        {
            BCLDebug.Trace("REMOTE", "Node " + name);
            BCLDebug.Trace("REMOTE", "text " + text);
            BCLDebug.Trace("REMOTE", "textLength " + textLength);
            BCLDebug.Trace("REMOTE", "size " + size);
            BCLDebug.Trace("REMOTE", "subType " + ((Enum)subType).ToString());
            BCLDebug.Trace("REMOTE", "nType " + ((Enum)nType).ToString());
            BCLDebug.Trace("REMOTE", "terminal " + terminal);
            BCLDebug.Trace("REMOTE", "prefixLength " + prefixLength);
            BCLDebug.Trace("REMOTE", "fEmpty " + fEmpty + "\n");
        }

        private String GetInvalidSyntaxMessage()
        {
            String lastProcessedTag = null;
            if (lastProcessed != null)
                lastProcessedTag = (lastProcessedEndElement ? "</" : "<") + lastProcessed + ">";
            return Environment.GetResourceString("XML_Syntax_InvalidSyntaxInFile", fileName, lastProcessedTag);
        }
    }

    internal class ConfigNode
    {
        String m_name = null;
        String m_value = null;
        ConfigNode m_parent = null;
        List<ConfigNode> m_children = new List<ConfigNode>(5);
        List<DictionaryEntry> m_attributes = new List<DictionaryEntry>(5);
        internal ConfigNode(String name, ConfigNode parent)
        {
            m_name = name;
            m_parent = parent;
        }

        internal String Name
        {
            get
            {
                return m_name;
            }
        }

        internal String Value
        {
            get
            {
                return m_value;
            }

            set
            {
                m_value = value;
            }
        }

        internal ConfigNode Parent
        {
            get
            {
                return m_parent;
            }
        }

        internal List<ConfigNode> Children
        {
            get
            {
                return m_children;
            }
        }

        internal List<DictionaryEntry> Attributes
        {
            get
            {
                return m_attributes;
            }
        }

        internal void AddChild(ConfigNode child)
        {
            child.m_parent = this;
            m_children.Add(child);
        }

        internal int AddAttribute(String key, String value)
        {
            m_attributes.Add(new DictionaryEntry(key, value));
            return m_attributes.Count - 1;
        }

        internal void ReplaceAttribute(int index, String key, String value)
        {
            m_attributes[index] = new DictionaryEntry(key, value);
        }

        internal void Trace()
        {
            BCLDebug.Trace("REMOTE", "************ConfigNode************");
            BCLDebug.Trace("REMOTE", "Name = " + m_name);
            if (m_value != null)
                BCLDebug.Trace("REMOTE", "Value = " + m_value);
            if (m_parent != null)
                BCLDebug.Trace("REMOTE", "Parent = " + m_parent.Name);
            for (int i = 0; i < m_attributes.Count; i++)
            {
                DictionaryEntry de = (DictionaryEntry)m_attributes[i];
                BCLDebug.Trace("REMOTE", "Key = " + de.Key + "   Value = " + de.Value);
            }

            for (int i = 0; i < m_children.Count; i++)
            {
                ((ConfigNode)m_children[i]).Trace();
            }
        }
    }
}