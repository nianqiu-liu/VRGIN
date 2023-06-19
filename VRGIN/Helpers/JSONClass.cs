using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VRGIN.Helpers
{
    public class JSONClass : JSONNode, IEnumerable
    {
        private Dictionary<string, JSONNode> m_Dict = new Dictionary<string, JSONNode>();

        public override JSONNode this[string aKey]
        {
            get
            {
                if (m_Dict.ContainsKey(aKey)) return m_Dict[aKey];
                return new JSONLazyCreator(this, aKey);
            }
            set
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = value;
                else
                    m_Dict.Add(aKey, value);
            }
        }

        public override JSONNode this[int aIndex]
        {
            get
            {
                if (aIndex < 0 || aIndex >= m_Dict.Count) return null;
                return m_Dict.ElementAt(aIndex).Value;
            }
            set
            {
                if (aIndex >= 0 && aIndex < m_Dict.Count)
                {
                    var key = m_Dict.ElementAt(aIndex).Key;
                    m_Dict[key] = value;
                }
            }
        }

        public override int Count => m_Dict.Count;

        public override IEnumerable<JSONNode> Children
        {
            get
            {
                foreach (var item in m_Dict) yield return item.Value;
            }
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            if (!string.IsNullOrEmpty(aKey))
            {
                if (m_Dict.ContainsKey(aKey))
                    m_Dict[aKey] = aItem;
                else
                    m_Dict.Add(aKey, aItem);
            }
            else
                m_Dict.Add(Guid.NewGuid().ToString(), aItem);
        }

        public override JSONNode Remove(string aKey)
        {
            if (!m_Dict.ContainsKey(aKey)) return null;
            var result = m_Dict[aKey];
            m_Dict.Remove(aKey);
            return result;
        }

        public override JSONNode Remove(int aIndex)
        {
            if (aIndex < 0 || aIndex >= m_Dict.Count) return null;
            var keyValuePair = m_Dict.ElementAt(aIndex);
            m_Dict.Remove(keyValuePair.Key);
            return keyValuePair.Value;
        }

        public override JSONNode Remove(JSONNode aNode)
        {
            try
            {
                var keyValuePair = m_Dict.Where((KeyValuePair<string, JSONNode> k) => k.Value == aNode).First();
                m_Dict.Remove(keyValuePair.Key);
                return aNode;
            }
            catch
            {
                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            foreach (var item in m_Dict) yield return item;
        }

        public override string ToString()
        {
            var text = "{";
            foreach (var item in m_Dict)
            {
                if (text.Length > 2) text += ", ";
                text = text + "\"" + Escape(item.Key) + "\":" + item.Value.ToString();
            }

            return text + "}";
        }

        public override string ToString(string aPrefix)
        {
            var text = "{ ";
            foreach (var item in m_Dict)
            {
                if (text.Length > 3) text += ", ";
                text = text + "\n" + aPrefix + "   ";
                text = text + "\"" + Escape(item.Key) + "\" : " + item.Value.ToString(aPrefix + "   ");
            }

            return text + "\n" + aPrefix + "}";
        }

        public override string ToJSON(int prefix)
        {
            var text = new string(' ', (prefix + 1) * 2);
            var text2 = "{ ";
            foreach (var item in m_Dict)
            {
                if (text2.Length > 3) text2 += ", ";
                text2 = text2 + "\n" + text;
                text2 += $"\"{item.Key}\": {item.Value.ToJSON(prefix + 1)}";
            }

            return text2 + "\n" + text + "}";
        }

        public override void Serialize(BinaryWriter aWriter)
        {
            aWriter.Write((byte)2);
            aWriter.Write(m_Dict.Count);
            foreach (var key in m_Dict.Keys)
            {
                aWriter.Write(key);
                m_Dict[key].Serialize(aWriter);
            }
        }
    }
}
