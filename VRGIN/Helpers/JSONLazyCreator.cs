namespace VRGIN.Helpers
{
    internal class JSONLazyCreator : JSONNode
    {
        private JSONNode m_Node;

        private string m_Key;

        public override JSONNode this[int aIndex]
        {
            get => new JSONLazyCreator(this);
            set
            {
                var jSONArray = new JSONArray();
                jSONArray.Add(value);
                Set(jSONArray);
            }
        }

        public override JSONNode this[string aKey]
        {
            get => new JSONLazyCreator(this, aKey);
            set
            {
                var jSONClass = new JSONClass();
                jSONClass.Add(aKey, value);
                Set(jSONClass);
            }
        }

        public override int AsInt
        {
            get
            {
                var aVal = new JSONData(0);
                Set(aVal);
                return 0;
            }
            set
            {
                var aVal = new JSONData(value);
                Set(aVal);
            }
        }

        public override float AsFloat
        {
            get
            {
                var aVal = new JSONData(0f);
                Set(aVal);
                return 0f;
            }
            set
            {
                var aVal = new JSONData(value);
                Set(aVal);
            }
        }

        public override double AsDouble
        {
            get
            {
                var aVal = new JSONData(0.0);
                Set(aVal);
                return 0.0;
            }
            set
            {
                var aVal = new JSONData(value);
                Set(aVal);
            }
        }

        public override bool AsBool
        {
            get
            {
                var aVal = new JSONData(false);
                Set(aVal);
                return false;
            }
            set
            {
                var aVal = new JSONData(value);
                Set(aVal);
            }
        }

        public override JSONArray AsArray
        {
            get
            {
                var jSONArray = new JSONArray();
                Set(jSONArray);
                return jSONArray;
            }
        }

        public override JSONClass AsObject
        {
            get
            {
                var jSONClass = new JSONClass();
                Set(jSONClass);
                return jSONClass;
            }
        }

        public JSONLazyCreator(JSONNode aNode)
        {
            m_Node = aNode;
            m_Key = null;
        }

        public JSONLazyCreator(JSONNode aNode, string aKey)
        {
            m_Node = aNode;
            m_Key = aKey;
        }

        private void Set(JSONNode aVal)
        {
            if (m_Key == null)
                m_Node.Add(aVal);
            else
                m_Node.Add(m_Key, aVal);
            m_Node = null;
        }

        public override void Add(JSONNode aItem)
        {
            var jSONArray = new JSONArray();
            jSONArray.Add(aItem);
            Set(jSONArray);
        }

        public override void Add(string aKey, JSONNode aItem)
        {
            var jSONClass = new JSONClass();
            jSONClass.Add(aKey, aItem);
            Set(jSONClass);
        }

        public static bool operator ==(JSONLazyCreator a, object b)
        {
            if (b == null) return true;
            return (object)a == b;
        }

        public static bool operator !=(JSONLazyCreator a, object b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return true;
            return (object)this == obj;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return "";
        }

        public override string ToString(string aPrefix)
        {
            return "";
        }

        public override string ToJSON(int prefix)
        {
            return "";
        }
    }
}
