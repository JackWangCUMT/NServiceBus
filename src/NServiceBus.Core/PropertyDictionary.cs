namespace NServiceBus
{
    using System.Collections;
    using System.Collections.Generic;

    sealed class PropertyDictionary : IReadOnlyDictionary<string, string>
    {
        string key;
        string value;
        PropertyDictionary tail;

        public static PropertyDictionary Empty = new PropertyDictionary();

        PropertyDictionary()
        {
        }

        PropertyDictionary(string key, string value, PropertyDictionary tail = null)
            : this()
        {
            this.key = key;
            this.value = value;
            this.tail = tail;
        }

        public bool TryGetValue(string requestedKey, out string foundValue)
        {
            if (key == requestedKey)
            {
                foundValue = value;
                return true;
            }
            if (tail != null)
            {
                return tail.TryGetValue(requestedKey, out foundValue);
            }
            foundValue = null;
            return false;
        }

        public PropertyDictionary Set(string newKey, string newValue)
        {
            return new PropertyDictionary(newKey, newValue, this);
        }

        bool Equals(PropertyDictionary other)
        {
            return string.Equals(key, other.key) && string.Equals(value, other.value) && Equals(tail, other.tail);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            if (tail == null)
            {
                yield break;
            }
            yield return new KeyValuePair<string, string>(key, value);
            foreach (var tailItem in tail)
            {
                yield return tailItem;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != this.GetType())
            {
                return false;
            }
            return Equals((PropertyDictionary) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (key != null ? key.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (value != null ? value.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (tail != null ? tail.GetHashCode() : 0);
                return hashCode;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool operator ==(PropertyDictionary left, PropertyDictionary right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PropertyDictionary left, PropertyDictionary right)
        {
            return !Equals(left, right);
        }

        public int Count
        {
            get
            {
                if (tail == null)
                {
                    return 0;
                }
                return 1 + tail.Count;
            }
        }

        public bool ContainsKey(string requestedKey)
        {
            if (key == requestedKey)
            {
                return true;
            }
            return tail != null && tail.ContainsKey(requestedKey);
        }
        
        public string this[string requestedKey]
        {
            get
            {
                string foundValue;
                if (!TryGetValue(requestedKey, out foundValue))
                {
                    throw new KeyNotFoundException("Requested key is not present in the dictionary: " + requestedKey);
                }
                return foundValue;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                if (tail == null)
                {
                    yield break;
                }
                yield return key;
                foreach (var k in tail.Keys)
                {
                    yield return k;
                }
            }
        }

        public IEnumerable<string> Values
        {
            get
            {
                if (tail == null)
                {
                    yield break;
                }
                yield return value;
                foreach (var v in tail.Values)
                {
                    yield return v;
                }
            }
        }
    }
}