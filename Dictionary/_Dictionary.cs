using System;
using System.Collections;
using System.Collections.Generic;
using static System.Collections.Generic.Dictionary<string, string>;

namespace Dictionary
{
    public class _Dictionary
    {
        private struct Entry
        {
            public int hashCode;    // Lower 31 bits of hash code, -1 if unused
            public int next;        // Index of next entry, -1 if last
            public string key;           // Key of entry
            public string value;         // Value of entry
        }

        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private IEqualityComparer<string> comparer;
        private KeyCollection keys;

        public _Dictionary() : this(0, null) { }
        public _Dictionary(int capacity) : this(capacity, null) { }
        public _Dictionary(IEqualityComparer<string> comparer) : this(0, comparer) { }
        public _Dictionary(int capacity, IEqualityComparer<string> comparer)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException();
            if (capacity > 0) Initialize(capacity);
            this.comparer = comparer ?? EqualityComparer<string>.Default;
        }
        public _Dictionary(IDictionary<string, string> dictionary) : this(dictionary, null) { }
        public _Dictionary(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) :
           this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException();
            }

            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }
        public string this[string key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;
                return default(string);
            }
            set
            {
                Insert(key, value, false);
            }
        }
        public void Add(string key, string value)
        {
            Insert(key, value, true);
        }
        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }
        public bool ContainsKey(string key)
        {
            return FindEntry(key) >= 0;
        }
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].next;
                        }
                        else
                        {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default(string);
                        entries[i].value = default(string);
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }
        public int Count
        {
            get { return count - freeCount; }
        }
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }
        private void CopyTo(KeyValuePair<string, string>[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentNullException();
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentNullException();
            }

            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair<string, string>(entries[i].key, entries[i].value);
                }
            }
        }
        public KeyCollection Keys
        {
            get
            {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }
        private void Initialize(int capacity)
        {
            int size = _HashHelpers.GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new Entry[size];
            freeList = -1;
        }
        private void Insert(string key, string value, bool add)
        {

            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (buckets == null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;
            int collisionCount = 0;
            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                    {
                        throw new ArgumentNullException();
                    }
                    entries[i].value = value;
                    version++;
                    return;
                }
                collisionCount++;
            }
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;

            }
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;
        }
        private void Resize()
        {
            Resize(_HashHelpers.ExpandPrime(count), false);
        }
        private void Resize(int newSize, bool forceNewHashCodes)
        {
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < count; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        newEntries[i].hashCode = (newEntries[i].key.GetHashCode() & 0x7FFFFFFF);
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }
        private int FindEntry(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) return i;
                }
            }
            return -1;
        }
        public sealed class KeyCollection : ICollection<string>, ICollection, IReadOnlyCollection<string>
        {
            private _Dictionary dictionary;

            public KeyCollection(_Dictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }
                this.dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public void CopyTo(string[] array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentNullException();
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentNullException();
                }

                int count = dictionary.count;
                Entry[] entries = dictionary.entries;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0) array[index++] = entries[i].key;
                }
            }
            public int Count
            {
                get { return dictionary.Count; }
            }
            bool ICollection<string>.IsReadOnly
            {
                get { return true; }
            }
            void ICollection<string>.Add(string item)
            {
                throw new ArgumentNullException();
            }
            void ICollection<string>.Clear()
            {
                throw new ArgumentNullException();
            }
            bool ICollection<string>.Contains(string item)
            {
                return dictionary.ContainsKey(item);
            }
            bool ICollection<string>.Remove(string item)
            {
                throw new ArgumentNullException();
                return false;
            }
            IEnumerator<string> IEnumerable<string>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }
            void ICollection.CopyTo(Array array, int index)
            {
                if (array == null)
                {
                    throw new ArgumentNullException();
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentNullException();
                }

                if (array.GetLowerBound(0) != 0)
                {
                    throw new ArgumentNullException();
                }

                if (index < 0 || index > array.Length)
                {
                    throw new ArgumentNullException();
                }

                if (array.Length - index < dictionary.Count)
                {
                    throw new ArgumentNullException();
                }

                string[] keys = array as string[];
                if (keys != null)
                {
                    CopyTo(keys, index);
                }
                else
                {
                    object[] objects = array as object[];
                    if (objects == null)
                    {
                        throw new ArgumentNullException();
                    }

                    int count = dictionary.count;
                    Entry[] entries = dictionary.entries;
                    try
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (entries[i].hashCode >= 0) objects[index++] = entries[i].key;
                        }
                    }
                    catch (ArrayTypeMismatchException)
                    {
                        throw new ArgumentNullException();
                    }
                }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            Object ICollection.SyncRoot
            {
                get { return ((ICollection)dictionary).SyncRoot; }
            }


            public struct Enumerator : IEnumerator<string>, System.Collections.IEnumerator
            {
                private _Dictionary dictionary;
                private int index;
                private int version;
                private string currentKey;

                public Enumerator(_Dictionary dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default(string);
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new ArgumentNullException();
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }

                    index = dictionary.count + 1;
                    currentKey = default(string);
                    return false;
                }

                public string Current
                {
                    get
                    {
                        return currentKey;
                    }
                }

                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.count + 1))
                        {
                            throw new ArgumentNullException();
                        }

                        return currentKey;
                    }
                }

                string IEnumerator<string>.Current => throw new NotImplementedException();

                void System.Collections.IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new ArgumentNullException();
                    }

                    index = 0;
                    currentKey = default(string);
                }
            }
        }
        public struct Enumerator : IEnumerator<KeyValuePair<string, string>>,
               IDictionaryEnumerator
        {
            private _Dictionary dictionary;
            private int version;
            private int index;
            private KeyValuePair<string, string> current;
            private int getEnumeratorRetType;

            internal const int DictEntry = 1;
            internal const int KeyValuePair = 2;

            public Enumerator(_Dictionary dictionary, int getEnumeratorRetType)
            {
                this.dictionary = dictionary;
                version = dictionary.version;
                index = 0;
                this.getEnumeratorRetType = getEnumeratorRetType;
                current = new KeyValuePair<string, string>();
            }
            public bool MoveNext()
            {
                if (version != dictionary.version)
                {
                    throw new ArgumentNullException();
                }
                while ((uint)index < (uint)dictionary.count)
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<string, string>(dictionary.entries[index].key, dictionary.entries[index].value);
                        index++;
                        return true;
                    }
                    index++;
                }
                index = dictionary.count + 1;
                current = new KeyValuePair<string, string>();
                return false;
            }
            public KeyValuePair<string, string> Current
            {
                get { return current; }
            }
            public void Dispose()
            {
            }
            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        throw new ArgumentNullException();
                    }

                    if (getEnumeratorRetType == DictEntry)
                    {
                        return new System.Collections.DictionaryEntry(current.Key, current.Value);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>(current.Key, current.Value);
                    }
                }
            }
            void IEnumerator.Reset()
            {
                if (version != dictionary.version)
                {
                    throw new ArgumentNullException();
                }

                index = 0;
                current = new KeyValuePair<string, string>();
            }
            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        throw new ArgumentNullException();
                    }

                    return new DictionaryEntry(current.Key, current.Value);
                }
            }
            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        throw new ArgumentNullException();
                    }

                    return current.Key;
                }
            }
            object IDictionaryEnumerator.Value
            {
                get
                {
                    if (index == 0 || (index == dictionary.count + 1))
                    {
                        throw new ArgumentNullException();
                    }
                    return current.Value;
                }
            }
        }

    }
}





