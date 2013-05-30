using System.Collections;

namespace Ego.PDF.PHP
{
    /// <summary>
    /// Represents a collection of associated String keys and Object values.
    /// </summary>
    public class OrderedMap : System.Collections.Specialized.NameObjectCollectionBase
    {
        #region Constants

        /// <summary>
        /// Constant used in sorting methods, compare items normally.
        /// </summary>
        public const int SORTREGULAR = 0;

        /// <summary>
        /// Constant used in sorting methods, compare items numerically.
        /// </summary>
        public const int SORTNUMERIC = 1;

        /// <summary>
        /// Constant used in sorting methods, compare items as strings.
        /// </summary>
        public const int SORTSTRING = 2;

        /// <summary>
        /// A string value that is used to evaluate whether a value matches the number format or not.
        /// </summary>
        private const string _NUMBERREGULAREXPRESSION = "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?$";

        #endregion

        #region Fields

        /// <summary>
        /// A integer value used as a index to an entry.
        /// </summary>
        private int _index;

        /// <summary>
        /// Creates random number generator. This field is used by the Shuffle method.
        /// </summary>
        private System.Random _random = new System.Random();

        #endregion

        #region Internal class (for sorting operations)

        /// <summary>
        /// Internal class used in sorting operations.
        /// It implements the System.IComparable interface that exposes the necessary sorting methods.
        /// </summary>
        internal class OrderedMapSortItem : System.IComparable
        {
            /// <summary>
            /// Contains the key of the element in the original OrderedMap.
            /// </summary>
            private string _key;

            /// <summary>
            /// Contains the value of the element in the original OrderedMap.
            /// </summary>
            private object _value;

            /// <summary>
            /// Specifies the sorting flags that will be used for the item. The valid values are:
            /// <list type="bullet">
            /// <item>OrderedMap.SORTREGULAR</item>
            /// <item>OrderedMap.SORTNUMERIC</item>
            /// <item>OrderedMap.SORTSTRING</item>
            /// </list>
            /// </summary>
            private int _sortFlags;

            /// <summary>
            /// Indicates whether to sort the item by its value or by its key. The values are:
            /// <list type="bullet">
            /// <item>true: Sorting will be done by value.</item>
            /// <item>false: Sorting will be done by key.</item>
            /// </list>
            /// </summary>
            private bool _byValue;

            /// <summary>
            /// Contains the name of the user-defined method that will be used as comparing method.
            /// </summary>
            private string _methodName;

            /// <summary>
            /// Constains the instance of the class that defines the user-defined method that will be used as comparing method.
            /// </summary>
            private object _instance;

            /// <summary>
            /// Creates a new OrderedMapSortItem with the specified key, value, sorting flags,
            /// and the value that indicates whether to sort the item by its value or by its key.
            /// </summary>
            /// <param name="entryKey">The key of the OrderedMap entry.</param>
            /// <param name="entryValue">The value of the OrderedMap entry.</param>
            /// <param name="sortFlags">The sorting flags that will be used for the element (SORTREGULAR, SORTNUMERIC, SORTSTRING)</param>
            /// <param name="byValue">The value that indicates whether to sort the item by its value (true) or by its key (false).</param>
            public OrderedMapSortItem(string entryKey, object entryValue, int sortFlags, bool byValue)
            {
                this._key = entryKey;
                this._value = entryValue;
                this._sortFlags = sortFlags;
                this._byValue = byValue;
                this._methodName = System.String.Empty;
                this._instance = null;
            }

            /// <summary>
            /// Creates a new OrderedMapSortItem with the specified key, value, comparing method name and instance that defines it.
            /// </summary>
            /// <param name="entryKey">The key of the OrderedMap entry.</param>
            /// <param name="entryValue">The value of the OrderedMap entry.</param>
            /// <param name="byValue">The value that indicates whether to sort the item by its value (True) or by its key (False).</param>
            /// <param name="methodName">The name of the user-defined method that will be used as comparing method.</param>
            /// <param name="instance">The instance of the class that defines the user-defined method that will be used as comparing method.</param>
            public OrderedMapSortItem(string entryKey, object entryValue, bool byValue, string methodName,
                                      object instance)
            {
                this._key = entryKey;
                this._value = entryValue;
                this._sortFlags = 0;
                this._byValue = byValue;
                this._methodName = methodName;
                this._instance = instance;
            }

            /// <summary>
            /// The key of the element in the original OrderedMap.
            /// </summary>
            public string Key
            {
                get { return this._key; }
            }

            /// <summary>
            /// The value of the element in the original OrderedMap.
            /// </summary>
            public object Value
            {
                get { return this._value; }
            }

            /// <summary>
            /// Compares the current OrderedMapSortItem with another OrderedMapSortItem.
            /// </summary>
            /// <param name="object2">The object to compare with this instance.</param>
            /// <returns>Returns a value that indicates the relative order of the comparands.</returns>
            public int CompareTo(object object2)
            {
                OrderedMapSortItem item2 = (OrderedMapSortItem) object2;

                //the two values (or keys) to compare.
                object value1 = this._byValue ? this._value : this._key;
                object value2 = this._byValue ? item2._value : item2._key;

                if (this._methodName == System.String.Empty && this._instance == null)
                {
                    string value1String = value1.ToString();
                    string value2String = value2.ToString();
                    bool matchValue1 = System.Text.RegularExpressions.Regex.IsMatch(value1String,
                                                                                    OrderedMap._NUMBERREGULAREXPRESSION);
                    bool matchValue2 = System.Text.RegularExpressions.Regex.IsMatch(value2String,
                                                                                    OrderedMap._NUMBERREGULAREXPRESSION);

                    //predefined comparing mechanisms.
                    switch (this._sortFlags)
                    {
                        case OrderedMap.SORTREGULAR: //Sort regular
                            if (matchValue1 && matchValue2)
                                goto case OrderedMap.SORTNUMERIC;
                            else if (value1 is OrderedMap)
                            {
                                return 1;
                            }
                            else if (value1 is bool)
                            {
                                if ((bool) value1)
                                    return 1;
                                else
                                    return -1;
                            }
                            else if (value2 is OrderedMap)
                            {
                                return -1;
                            }
                            else if (value2 is bool)
                            {
                                if ((bool) value2)
                                    return -1;
                                else
                                    return 1;
                            }
                            else if (matchValue1)
                                return 1;
                            else if (matchValue2)
                                return -1;
                            else goto default;

                        case OrderedMap.SORTNUMERIC: //Sort numeric
                            //if elements are not numbers, it is the same as zero.
                            double value1Double = (matchValue1) ? System.Convert.ToDouble(value1) : 0;
                            double value2Double = (matchValue2) ? System.Convert.ToDouble(value2) : 0;

                            if (value1Double < value2Double)
                                return -1;
                            else if (value1Double > value2Double)
                                return 1;
                            else
                                return 0;

                        default: //Sort string
                            return System.String.CompareOrdinal(value1String, value2String);
                    }
                }
                else
                {
                    //use a user-defined comparing method.
                    System.Type theType = this._instance.GetType();
                    System.Reflection.MethodInfo callbackMethod = theType.GetMethod(this._methodName);

                    object[] parameters;
                    if (this._byValue)
                        parameters = new object[] {value1, value2};
                    else
                    {
                        parameters = new object[]
                            {
                                OrderedMap.IsKeyInteger((string) value1) ? System.Convert.ToInt32(value1) : value1,
                                OrderedMap.IsKeyInteger((string) value2) ? System.Convert.ToInt32(value2) : value2
                            };
                    }

                    return (int) callbackMethod.Invoke(this._instance, parameters);
                }
            }
        }

        #endregion

        #region Class Constructors

        /// <summary>
        /// Creates a new empty OrderedMap.
        /// </summary>
        public OrderedMap()
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            this._index = 0;
        }

        /// <summary>
        /// Adds the values from the ICollection into the new OrderedMap.
        /// </summary>
        /// <param name="collection">The ICollection that contains the values.</param>
        /// <param name="isReadOnly">A boolean value that indicates whether the new instance is read-only.</param>
        public OrderedMap(System.Collections.ICollection collection, bool isReadOnly)
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            int keys = 0;
            foreach (object element in collection)
            {
                this.BaseAdd(keys.ToString(), element);
                keys++;
            }
            base.IsReadOnly = isReadOnly;
            this._index = 0;
        }

        /// <summary>
        /// Adds entries from an IDictionary into the new OrderedMap.
        /// </summary>
        /// <param name="dictionary">The IDictionary that contains the entries.</param>
        /// <param name="isReadOnly">A boolean value that indicates whether the new instance is read-only.</param>
        public OrderedMap(System.Collections.IDictionary dictionary, bool isReadOnly)
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                this.BaseAdd(System.Convert.ToString(entry.Key), entry.Value);
            }
            base.IsReadOnly = isReadOnly;
            this._index = 0;
        }

        /// <summary>
        /// Adds entries from a NameValueCollection into the new OrderedMap.
        /// </summary>
        /// <param name="collection">The NameValueCollection that contains the entries.</param>
        /// <param name="isReadOnly">A boolean value that indicates whether the new instance is read-only.</param>
        public OrderedMap(System.Collections.Specialized.NameValueCollection collection, bool isReadOnly)
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            for (int index = 0; index < collection.Count; index++)
            {
                System.Collections.DictionaryEntry entry = new System.Collections.DictionaryEntry();
                entry.Key = collection.Keys[index];
                entry.Value = collection[collection.Keys[index]];
                this.BaseAdd(System.Convert.ToString(entry.Key), entry.Value);
            }
            base.IsReadOnly = isReadOnly;
            this._index = 0;
        }

        /// <summary>
        /// Creates a new OrderedMap with the contents of the specified OrderedMap.
        /// </summary>
        /// <param name="orderedMap">The OrderedMap used to create the new OrderedMap.</param>
        /// <param name="isReadOnly">A boolean value that indicates whether the new instance is read-only.</param>
        public OrderedMap(OrderedMap orderedMap, bool isReadOnly)
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            foreach (string key in orderedMap)
            {
                this.BaseAdd(key, orderedMap[key]);
            }
            base.IsReadOnly = isReadOnly;
            this._index = 0;
        }

        /// <summary>
        /// Creates a new OrderedMap object containing the specified parameters as its elements.
        /// </summary>
        /// <param name="parameters">The elements used to create the new OrderedMap. Each element will
        /// be an entry. If an element is an array, the first element is taken as the key and the second
        /// element will be the value. Values are arrays when working with multidimensional arrays.
        /// <br>
        /// For example, the parameters <code>new System.Object[] {1,2,3,4,5}</code> will create a OrderedMap with the
        /// keys [0,1,2,3,4] and the values [1,2,3,4,5]. Similarly, the parameters <code>new System.Object[] 
        /// {new System.Object[] {"a", 1}, new System.Object[] {"b", 2}}</code> will create a OrderedMap with the keys
        /// ["a", "b"] and the values [1, 2].
        /// </br>
        /// </param>
        /// <returns>A new NameObjectCollection object containing the elements specified in the parameters parameter.</returns>
        public OrderedMap(params object[] args)
            : base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
        {
            int keyIndex = 0, maxIndex = int.MinValue;
            bool useMaxIndex = false;
            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] is object[])
                {
                    object theKey = ((object[]) args[index])[0];
                    object theValue = ((object[]) args[index])[1];
                    if (OrderedMap.IsKeyInteger(theKey.ToString()))
                    {
                        int intKey = theKey is int ? (int) theKey : System.Convert.ToInt32(theKey);
                        if (intKey > maxIndex)
                        {
                            maxIndex = intKey;
                            useMaxIndex = true;
                        }
                    }
                    this[theKey] = theValue;
                }
                else
                {
                    keyIndex = useMaxIndex ? (maxIndex += 1) : keyIndex;
                    this[keyIndex] = args[index];
                    keyIndex++;
                }
            }

            this._index = 0;
        }

        #endregion

        #region Public Indexers

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public object this[string key]
        {
            get { return (this.BaseGet(key)); }
            set
            {
                string newKey = key == null ? this._GetMaxIntegerKey() : key;
                this.BaseSet(newKey, value);
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public object this[int key]
        {
            get { return (this.BaseGet(key.ToString())); }
            set { this.BaseSet(key.ToString(), value); }
        }

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public object this[object key]
        {
            get { return (this.BaseGet(key.ToString())); }
            set
            {
                string newKey = key == null ? this._GetMaxIntegerKey() : key.ToString();
                this.BaseSet(newKey, value);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the value of the current element in this OrderedMap.
        /// </summary>
        /// <remarks>
        /// If the internal index is not a valid index in this OrderedMap, this property returns false. 
        /// </remarks>
        public object Current
        {
            get { return (this._index >= this.Count) || (this._index < 0) ? false : this.GetValueAt(this._index); }
        }

        /// <summary>
        /// Gets a value indicating if this OrderedMap contains keys that are not null.
        /// </summary>
        public bool HasKeys
        {
            get { return (this.BaseHasKeys()); }
        }

        /// <summary>
        /// Gets a string array that contains all the keys in this OrderedMap.
        /// </summary>
        public new string[] Keys
        {
            get { return (this.BaseGetAllKeys()); }
        }

        /// <summary>
        /// Gets an object array that contains all the values in this OrderedMap.
        /// </summary>
        public object[] Values
        {
            get { return (this.BaseGetAllValues()); }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the maximum integer key that can be used for new elements.
        /// </summary>
        /// <returns>The maximum integer key that can be used for new elements.</returns>
        private string _GetMaxIntegerKey()
        {
            string maxIndex = "-1";
            for (int index = this.Count - 1; index >= 0; index--)
            {
                string key = this.GetKeyAt(index);
                if (OrderedMap.IsKeyInteger(key))
                {
                    maxIndex = key;
                    break;
                }
            }
            return System.Convert.ToString(System.Convert.ToInt32(maxIndex) + 1);
        }

        /// <summary>
        /// Replaces a character of the specified string at the specified position by a new character. 
        /// If <code>index</code> is greater than the string length, then the string will be right padded with spaces.
        /// </summary>
        /// <param name="currentValue">The string value that will be changed.</param>
        /// <param name="index">The index in the string value of the character to be replaced.</param>
        /// <param name="newValue">The character to be used to replace the specified character.</param>
        /// <returns>Returns the new string.</returns>
        private string _ReplaceCharAt(string currentValue, int index, char newValue)
        {
            System.Text.StringBuilder stringValue = new System.Text.StringBuilder(currentValue);
            //make sure StringBuilder is big enough to hold the changed string.
            if (stringValue.Length < index + 1)
            {
                int oldLenght = stringValue.Length;
                stringValue.Length = index + 1;
                //replace only the new substring.
                stringValue.Replace('\x00', '\x20', oldLenght, stringValue.Length - oldLenght);
            }
            //the character of the string value pointed by the next index should 
            //be replaced by the first character of string representation of the new value.
            stringValue[index] = newValue;
            return stringValue.ToString();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an entry to this OrderedMap.
        /// </summary>
        /// <param name="key">The key of the new entry.</param>
        /// <param name="value">The value of the new entry.</param>
        public void Add(string key, object value)
        {
            this.BaseAdd(key, value);
        }

        /// <summary>
        /// Adds an entry to this OrderedMap.
        /// </summary>
        /// <param name="key">The key of the new entry.</param>
        /// <param name="value">The value of the new entry.</param>
        public void Add(int key, object value)
        {
            this.BaseAdd(key.ToString(), value);
        }

        /// <summary>
        /// Clears all the elements in this OrderedMap.
        /// </summary>
        public void Clear()
        {
            this.BaseClear();
        }

        /// <summary>
        /// Returns the current key and value pair of this OrderedMap and advances the internal index by one.
        /// </summary>
        /// <returns>Returns the current key and value pair from this OrderedMap</returns>
        public OrderedMap Each()
        {
            OrderedMap each = null;
            if ((this._index < this.Count) && (this._index >= 0))
            {
                System.Collections.DictionaryEntry entry = this.GetEntryAt(this._index);
                each = new OrderedMap(
                    new object[] {1, entry.Value},
                    new object[] {"value", entry.Value},
                    new object[] {0, entry.Key},
                    new object[] {"key", entry.Key});

                this._index++;
            }
            return each;
        }

        /// <summary>
        /// Sets the current element to the end of this OrderedMap and returns that element.
        /// </summary>
        /// <returns>The last element of this OrderedMap.</returns>
        public object End()
        {
            this._index = this.Count - 1;
            return this.GetValueAt(this._index);
        }

        /// <summary>
        /// Gets the entry at the specified position.
        /// </summary>
        /// <param name="index">The specified position of the entry in this OrderedMap.</param>
        /// <returns>Returns the entrey at the specified position.</returns>
        public System.Collections.DictionaryEntry GetEntryAt(int index)
        {
            System.Collections.DictionaryEntry entry = new System.Collections.DictionaryEntry();
            entry.Key = this.BaseGetKey(index);
            entry.Value = this.BaseGet(index);
            return (entry);
        }

        /// <summary>
        /// Gets the key at the specified position.
        /// </summary>
        /// <param name="index">The position of the key in this OrderedMap.</param>
        /// <returns>Returns the key of the entry at the specified position.</returns>
        public string GetKeyAt(int index)
        {
            return this.BaseGetKey(index);
        }

        /// <summary>
        /// Returns a new OrderedMap containing all the keys of this OrderedMap.
        /// </summary>
        /// <param name="filter">A value used to filter the keys for the specified value. If null, no filtering is done.</param>
        /// <returns>Returns a new OrderedMap with the keys of this OrderedMap.</returns>
        public OrderedMap GetKeysOrderedMap(object filter)
        {
            OrderedMap newOrderedMap = null;

            if (this.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                foreach (string theKey in this.Keys)
                {
                    if (filter == null)
                        newOrderedMap[null] = theKey;
                    else
                    {
                        if (this[theKey].ToString().Equals(filter.ToString()))
                            newOrderedMap[null] = theKey;
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Returns a randomly-choosen key from this OrderedMap.
        /// </summary>
        /// <returns>Returns a randomly-choosen key.</returns>
        public object GetRandomKey()
        {
            object key = null;
            if (this.Count > 0)
            {
                System.Random random = new System.Random();
                int randKeyIndex = random.Next(this.Count - 1);
                key = this.GetKeyAt(randKeyIndex);
            }
            return key;
        }

        /// <summary>
        /// Returns an OrderedMap containing randomly-choosen keys from this OrderedMap.
        /// </summary>
        /// <param name="numKeys">The number of keys to obtain.</param>
        /// <returns>Returns a new OrderedMap containing randomly-choosen keys.</returns>
        public OrderedMap GetRandomKeys(int numKeys)
        {
            OrderedMap newOrderedMap = null;
            if (this.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                System.Random random = new System.Random();
                for (int index = 0; index < numKeys; index++)
                {
                    int randKeyIndex = random.Next(this.Count - 1);
                    newOrderedMap[null] = this.GetKeyAt(randKeyIndex);
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Gets the value at the specified position.
        /// </summary>
        /// <param name="index">The position of the value in this OrderedMap.</param>
        /// <returns>Returns the value of the entry at the specified position.</returns>
        public object GetValueAt(int index)
        {
            return this.BaseGet(index);
        }

        /// <summary>
        /// Returns a new OrderedMap containing all the values of this OrderedMap.
        /// </summary>
        /// <returns></returns>
        public OrderedMap GetValuesOrderedMap()
        {
            OrderedMap newOrderedMap = null;
            if (this.Count > 0)
            {
                newOrderedMap = new OrderedMap(this.Values, false);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Returns the key of the current element of this OrderedMap.
        /// </summary>
        /// <returns>Returns the key of the current element of this OrderedMap.</returns>
        public string Key()
        {
            return this.GetKeyAt(this._index);
        }

        /// <summary>
        /// Checks if the specified key exists in this OrderedMap.
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        /// <returns>Returns true if the specified key exists in this OrderedMap, false otherwise.</returns>
        public bool KeyExists(object key)
        {
            return this[key] == null ? false : true;
        }

        /// <summary>
        /// Advances the internal index by one, returns the element in that position.
        /// </summary>
        /// <returns>The element in the next position that's pointed by the interanl index, 
        /// or false if the internal index is the end of this OrderedMap.</returns>
        public object Next()
        {
            this._index++;
            return this.Current;
        }

        /// <summary>
        /// Pops the last value of this OrderedMap.
        /// </summary>
        /// <returns>Returns the last value (if any).</returns>
        public object Pop()
        {
            object theValue = null;
            if (this.Count > 0)
            {
                theValue = this.GetValueAt(this.Count - 1);
                this.RemoveAt(this.Count - 1);
                this.Reset();
            }
            return theValue;
        }

        /// <summary>
        /// Rewinds the internal index by one, returns the element in that position.
        /// </summary>
        /// <returns>The element in the previous position that's pointed by the internal index, 
        /// or false if there are no more elements.</returns>
        public object Previous()
        {
            this._index--;
            return this.Current;
        }

        /// <summary>
        /// Pushess one or more values to the end of this OrderedMap.
        /// </summary>
        /// <param name="args">The variable length array of arguments that contain the values to be pushed.</param>
        /// <returns>Returns the number of elements after the push operation.</returns>
        public int Push(params object[] args)
        {
            foreach (var t in args)
            {
                this[null] = t;
            }
            return this.Count;
        }

        /// <summary>
        /// This method is used to randomly re-order the elements of a OrderedMap. It returns a random number (-1, 0, 1).
        /// </summary>
        /// <param name="dummy1">Unused parameter, it exists only to comply with sorting methods requirements.</param>
        /// <param name="dummy2">Unused parameter, it exists only to comply with sorting methods requirements.</param>
        /// <returns>Returns a random number between -1 a 1 (-1, 0, 1).</returns>
        public int RamdomCompare(object dummy1, object dummy2)
        {
            var compare = (this._random.Next(0, 3)) - 1;
            return compare;
        }

        /// <summary>
        /// Removes an entry with the specified key from this OrderedMap.
        /// </summary>
        /// <param name="key">The key of the entry that will be removed.</param>
        public void Remove(string key)
        {
            this.BaseRemove(key);
        }

        /// <summary>
        /// Removes an entry with the specified key from this OrderedMap.
        /// </summary>
        /// <param name="key">The key of the entry that will be removed.</param>
        public void Remove(int key)
        {
            this.BaseRemove(key.ToString());
        }

        /// <summary>
        /// Removes an entry in the specified index from this OrderedMap.
        /// </summary>
        /// <param name="index">The index of the entry that will be removed.</param>
        public void RemoveAt(int index)
        {
            this.BaseRemoveAt(index);
        }

        /// <summary>
        /// Sets the internal index of this OrderedMap to the first element.
        /// </summary>
        /// <returns>The element in the first position of this OrderedMap.</returns>
        public object Reset()
        {
            this._index = 0;
            return this.Current;
        }

        /// <summary>
        /// Searchs the specified string in this OrderedMap. This method compares the string representation of the elements with the specified string.
        /// </summary>
        /// <param name="toSearch">The string to be searched in this OrderedMap.</param>
        /// <returns>Returns the index of the found value, or -1 if the string is not found.</returns>
        public int SearchStringRepresentation(string toSearch)
        {
            int result = -1;
            for (int index = 0; index < this.Count; index++)
            {
                if (this.GetValueAt(index).ToString().Equals(toSearch))
                {
                    result = index;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Searchs the specified entry in this OrderedMap. This method compares the string representation of the entries with the specified entry.
        /// </summary>
        /// <param name="toSearch">The entry to be searched in this OrderedMap.</param>
        /// <returns>Returns the index of the found entry, or -1 if the entry is not found.</returns>
        public int SearchStringRepresentation(System.Collections.DictionaryEntry toSearch)
        {
            int result = -1;
            for (int index = 0; index < this.Count; index++)
            {
                System.Collections.DictionaryEntry entry = this.GetEntryAt(index);
                if (entry.Value.ToString().Equals(toSearch.Value.ToString()) &&
                    entry.Key.ToString().Equals(toSearch.Key.ToString()))
                {
                    result = index;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Searchs this OrderedMap for the specified value. 
        /// An element is considered equals to another when the type and value are the same.
        /// </summary>
        /// <param name="theValue">The value to be searched in this OrderedMap.</param>
        /// <returns>Returns a string value that contains the key if the element is found, or null otherwise.</returns>
        public string Search(object theValue)
        {
            string result = null;
            foreach (var key in this.Keys)
            {
                object currentValue = this[key];

                if (currentValue.GetType() == theValue.GetType()) //type checking
                {
                    //the operand == is type-dependant, for that reason castings are made according to the values types.
                    bool equals = false;
                    //for predefined value types, the equality operator (==) returns true if the values of its operands are equal, false otherwise.
                    if (currentValue is bool)
                        equals = (bool) currentValue == (bool) theValue;
                    else if (currentValue is int || currentValue is float || currentValue is double)
                        equals = (int) currentValue == (int) theValue;
                    else if (currentValue is string)
                        //for the string type, == compares the values of the strings.
                        equals = (string) currentValue == (string) theValue;
                    else if (currentValue is OrderedMap)
                        //for the OrderedMap type, the values of the OrderedMaps are compared.
                        equals = ((OrderedMap) currentValue).ToStringContents() ==
                                 ((OrderedMap) theValue).ToStringContents();
                    else
                        //for reference types other than string, == returns true if its two operands refer to the same object
                        equals = currentValue == theValue;

                    if (equals)
                    {
                        result = key;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Sums the numeric values of this OrderedMap.
        /// </summary>
        /// <returns>Returns the sum of the numeric values of this OrderedMap.</returns>
        public double SumValues()
        {
            double theSum = 0;
            if (this.Count > 0)
            {
                foreach (object theValue in this.Values)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(theValue.ToString(),
                                                                     OrderedMap._NUMBERREGULAREXPRESSION))
                        theSum += System.Convert.ToDouble(theValue);
                }
            }
            return theSum;
        }

        /// <summary>
        /// Returns a string that represents this OrderedMap.
        /// </summary>
        public override string ToString()
        {
            return "Array";
        }

        /// <summary>
        /// Returns a human-readable string representation of the contents of this OrderedMap.
        /// </summary>
        /// <returns>Returns a string with the contents of this OrderedMap.</returns>
        public string ToStringContents()
        {
            int indentLevel = 0;
            return this.ToStringContents(ref indentLevel).TrimEnd();
        }

        /// <summary>
        /// Returns a human-readable string representation of the contents of this OrderedMap.
        /// </summary>
        /// <param name="indentLevel">This argument is used for indentation.</param>
        /// <returns>Returns a string with the contents of this OrderedMap.</returns>
        public string ToStringContents(ref int indentLevel)
        {
            indentLevel++;
            string indentSpaces = new string(' ', (indentLevel - 1)*4);
            string toStringContents = "Array\r\n" + indentSpaces + "(\r\n";
            foreach (string theKey in this.Keys)
            {
                object theValue = this[theKey];
                indentSpaces = new string(' ', indentLevel*4);
                if (theValue is OrderedMap)
                {
                    indentLevel++;
                    toStringContents = toStringContents + indentSpaces + "[" + theKey + "] => " +
                                       ((OrderedMap) theValue).ToStringContents(ref indentLevel);
                    indentLevel--;
                }
                else
                {
                    string theValueString = theValue is bool ? (bool) theValue ? "1" : "" : theValue.ToString();
                    toStringContents = toStringContents + indentSpaces + "[" + theKey + "] => " + theValueString +
                                       "\r\n";
                }
            }
            indentSpaces = new string(' ', (indentLevel - 1)*4);
            indentLevel--;
            toStringContents = toStringContents + indentSpaces + ")\r\n\r\n";
            return toStringContents;
        }

        /// <summary>
        /// Applies the specified method of the specified instance to all elements of this OrderedMap.
        /// </summary>
        /// <param name="methodName">The method name that will be called.</param>
        /// <param name="data">The optional data (null if nothing) that will be passed to the method.</param>
        /// <param name="instance">The instance where the method to be called is defined.</param>
        /// <returns>Returns true if success, false otherwise.</returns>
        public bool Walk(string methodName, object data, object instance)
        {
            bool result = false;
            if (this.Count > 0)
            {
                try
                {
                    System.Type theType = instance.GetType();
                    System.Reflection.MethodInfo callbackMethod = theType.GetMethod(methodName);

                    foreach (string theKey in this.Keys)
                    {
                        object[] parameters = data == null
                                                  ? new object[] {this[theKey], theKey}
                                                  : new object[] {this[theKey], theKey, data};
                        callbackMethod.Invoke(instance, parameters);
                    }
                    result = true;
                }
                catch (System.Exception exception)
                {
                    throw exception;
                }
            }

            return result;
        }

        #endregion

        #region Public Methods to work with multidimensional OrderedMaps

        /// <summary>
        /// Sets a value in a multidimensional OrderedMap. 
        /// </summary>
        /// <param name="args">The parameter <code>args</code> is an array of objects with the following form:
        /// args(value, key1, ..., keyn-1, keyn). Where value (the first element) is the value to be set, the rest of the elements
        /// are the keys, each key represents a dimension. 
        /// For instance, a PHP source code that look like this: <code>arr[1][3][4][6] = "value";</code> would be converted using this method
        /// like this: <code>arr.SetValue("value", 1, 3, 4, 6);</code>.</param>
        public void SetValue(params object[] args)
        {
            if (args.Length < 2) return;
            if (args.Length == 2)
            {
                this[args[1]] = args[0];
                return;
            }

            //if there are at least two indexers go on.

            string theKey = "";
            //loop over the specified keys creating new OrderedMaps (i.e. dimensions) when necessary.
            OrderedMap currentOrderedMap = this;
            for (int index = 1; index < args.Length - 1; index++)
            {
                //if a key is not specified (null), it should be calculated.
                theKey = args[index] == null ? currentOrderedMap._GetMaxIntegerKey() : args[index].ToString();

                if (currentOrderedMap[theKey] == null)
                {
                    //specified element does not exist, create a new OrderedMap (i.e. a new dimension).
                    currentOrderedMap[theKey] = new OrderedMap();
                }
                else
                {
                    //current element already exists in OrderedMap.
                    if (currentOrderedMap[theKey] is string)
                    {
                        //current element is not a OrderedMap but a string. If possible, change the string value and return.
                        object newValue;
                        if (index == args.Length - 1)
                        {
                            //the string value should be changed by the new value.
                            newValue = args[0];
                        }
                        else if (index == args.Length - 2)
                        {
                            newValue = this._ReplaceCharAt((string) currentOrderedMap[theKey], (int) args[index + 1],
                                                           args[0].ToString()[0]);
                        }
                        else
                        {
                            //the indexers are not valid in the string value, ignore this case and return.
                            return;
                        }

                        //update the string value and return.
                        currentOrderedMap[theKey] = newValue;
                        return;
                    }
                    else
                    {
                        if (!(currentOrderedMap[theKey] is OrderedMap))
                        {
                            //current element is not a string nor an OrderedMap. It is not possible to use that element as an array.
                            throw new System.Exception("Warning: Cannot use a scalar value as an array");
                        }
                    }
                }

                //move to next dimension, current element is an OrderedMap (either a new or an existing one).
                currentOrderedMap = (OrderedMap) currentOrderedMap[theKey];
            } //end for

            //current OrderedMap represents the last dimension, set a new entry in the current OrderedMap,
            //formed by the last key (if any) and the specified value.
            currentOrderedMap[args[args.Length - 1]] = args[0];
        }

        /// <summary>
        /// Gets a value in a multidimensional OrderedMap.
        /// </summary>
        /// <param name="args">The parameter <code>args</code> is an array of objects which contains the keys used to obtain the value.</param>
        /// <returns>The value stored in the OrderedMap.</returns>
        public object GetValue(params object[] args)
        {
            //loop over this OrderedMap elements, if the current element is an OrderedMap, it is considered as another dimension.
            OrderedMap currentOrderedMap = this;
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (currentOrderedMap[args[index]] != null)
                {
                    if (currentOrderedMap[args[index]] is string)
                    {
                        //current element is not a OrderedMap but a string, if possible return the substring.
                        if (index == args.Length - 2)
                        {
                            //arr[0][1]: where arr[0] is a string, the second indexer (1) is actually a string indexer.
                            return ((string) currentOrderedMap[args[index]]).Substring((int) args[index + 1], 1);
                        }
                        else
                        {
                            //arr[0][1][2]: where arr[0] is a string, the third indexer (2) is not valid.
                            return string.Empty;
                        }
                    }
                    else
                    {
                        //move to next dimension (current element is an OrderedMap).
                        currentOrderedMap = (OrderedMap) currentOrderedMap[args[index]];
                    }
                }
                else
                {
                    //specified element does not exist in OrderedMap
                    return string.Empty;
                }
            }
            //current OrderedMap represents the last dimension, return the value associated to the last key.
            return currentOrderedMap[args[args.Length - 1]];
        }

        #endregion

        #region Private Static Methods

        /// <summary>
        /// Gets the absolute values of the two positions (those positions usually indicate a subrange) in a range that goes from zero to <code>count</code>.
        /// </summary>
        /// <param name="offset">The first position, it is used to indicate where the subrange starts.</param>
        /// <param name="length">The length of the subrange.</param>
        /// <param name="count">The total lenght of the range used to calculate the absolute positions.</param>
        private static void _GetAbsPositions(ref int offset, ref int length, int count)
        {
            int theOffset = 0, theLength = 0;

            theOffset = offset < 0 ? count + offset : offset;
            theOffset = theOffset < 0 ? 0 : theOffset;
            theOffset = theOffset > count ? count : theOffset;

            theLength = length < 0 ? (count + length) - theOffset : length;
            theLength = theLength < 0 ? 0 : theLength;
            theLength = theLength + theOffset > count ? count - theOffset : theLength;

            offset = theOffset;
            length = theLength;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Appends the contents of the second OrderedMap to the first OrderedMap into a new OrderedMap. 
        /// </summary>
        /// <param name="input1">The first OrderedMap.</param>
        /// <param name="input2">The second OrderedMap to be appended.</param>
        /// <returns>Returns a new OrderedMap with the appended OrderedMaps.</returns>
        public static OrderedMap Append(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;

            //if at least one of the OrderedMaps is not null
            if (input1 != null || input2 != null)
            {
                newOrderedMap = new OrderedMap();
                if (input1 != null)
                    newOrderedMap = new OrderedMap(input1, false);

                if (input2 != null)
                {
                    foreach (string key in input2.Keys)
                    {
                        if (newOrderedMap[key] == null)
                            newOrderedMap[key] = input2[key];
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Changes the case of all string keys of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap that contains the keys to be changed.</param>
        /// <param name="arrayCase">Indicates whether to change the keys to lowercase (0) or to uppercase (any other integer value)</param>
        /// <returns>Returns a new OrderedMap with all string keys lowercased or uppercased.</returns>
        public static OrderedMap ChangeCase(OrderedMap input, int arrayCase)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                foreach (string key in input.Keys)
                {
                    string newKey = key;
                    newKey = (arrayCase == 0 ? key.ToLower() : key.ToUpper());
                    newOrderedMap[newKey] = input[key];
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Splits the specified OrderedMap into chunks
        /// </summary>
        /// <param name="input">The OrderedMap to be splitted.</param>
        /// <param name="size">The size of each chunk.</param>
        /// <param name="preserveKeys">If true, the original keys will be preserved, otherwise new integer indices will be used.</param>
        /// <returns>Returns a new OrderedMap containing all the chunks.</returns>
        public static OrderedMap Chunk(OrderedMap input, int size, bool preserveKeys)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                int totalChunks = input.Count/size;
                totalChunks += (input.Count%size) > 0 ? 1 : 0;
                for (int index = 0; index < totalChunks; index++)
                {
                    OrderedMap theChunk = new OrderedMap();
                    for (int chunkIndex = 0; chunkIndex < size; chunkIndex++)
                    {
                        if ((index*size) + chunkIndex < input.Count)
                        {
                            string key = "";
                            if (preserveKeys)
                                key = input.GetKeyAt((index*size) + chunkIndex);
                            else
                                key = chunkIndex.ToString();

                            theChunk[key] = input.GetValueAt((index*size) + chunkIndex);
                        }
                        else
                            break;
                    }
                    newOrderedMap[index] = theChunk;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Returns the number of elements of the specified variable.
        /// </summary>
        /// <param name="variable">The variable which contains the elements.</param>
        /// <returns>Returns the number of elemets of the specified variable.</returns>
        /// <remarks>Even though this method is meant to be used with a OrderedMap, this is not mandatory, it can be used with other variable types.</remarks>
        public static int CountElements(object variable)
        {
            //ADDED ICOLLECTION mregsoe
            int count = 0;
            if (variable != null)
            {
                if (variable is OrderedMap)
                    count = ((OrderedMap) variable).Count;
                else if (variable is ICollection)
                    count = (variable as ICollection).Count;
                else
                    return 1;
            }

            return count;
        }

        /// <summary>
        /// Counts all the values of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap that contains the values to be counted.</param>
        /// <returns>Returns a new OrderedMap which contains the values and their frecuency.</returns>
        public static OrderedMap CountValues(OrderedMap input)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                foreach (object theValue in input.Values)
                {
                    if (newOrderedMap[theValue] != null)
                        newOrderedMap[theValue] = ((int) newOrderedMap[theValue]) + 1;
                    else
                        newOrderedMap[theValue] = 1;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Creates a new OrderedMap with a consecutive range of numbers.
        /// </summary>
        /// <param name="low">The first value of the range.</param>
        /// <param name="high">The last value of the range.</param>
        /// <returns>Returns a new OrderedMap with a consecutive range of numbers.</returns>
        public static OrderedMap CreateRange(int low, int high)
        {
            OrderedMap newOrderedMap = new OrderedMap();
            bool increment = high >= low ? true : false;

            for (int index = low; increment ? index <= high : index >= high; index += increment ? 1 : -1)
            {
                newOrderedMap[null] = index;
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Creates a new OrderedMap with a consecutive range of characters (actually strings are used for compatibility).
        /// </summary>
        /// <param name="lowChar">The first value of the range.</param>
        /// <param name="highChar">The last value of the range.</param>
        /// <returns>Returns a new OrderedMap with a consecutive range of characters.</returns>
        public static OrderedMap CreateRange(string lowChar, string highChar)
        {
            OrderedMap newOrderedMap = new OrderedMap();
            int lowCharInt = (int) lowChar[0], highCharInt = (int) highChar[0];
            bool increment = highCharInt >= lowCharInt ? true : false;

            for (int index = lowCharInt;
                 increment ? index <= highCharInt : index >= highCharInt;
                 index += increment ? 1 : -1)
            {
                newOrderedMap[null] = new string(new char[] {(char) index});
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the difference of two OrderedMaps. That is, elements of the first OrderedMap not present in the second OrderedMap.
        /// </summary>
        /// <param name="input1">The OrderedMap that contains the elements to be searched.</param>
        /// <param name="input2">The OrderedMap where the elements of the first element will be searched.</param>
        /// <returns>Returns a new OrderedMap that contains the elements of the first OrderedMap not present in the second OrderedMap.</returns>
        public static OrderedMap Difference(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;
            for (int index = 0; index < input1.Count; index++)
            {
                System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
                if (input2.SearchStringRepresentation(entry.Value.ToString()) == -1)
                {
                    if (newOrderedMap == null) newOrderedMap = new OrderedMap();
                    newOrderedMap[entry.Key] = entry.Value;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the difference of two or more OrderedMaps. That is, elements of the first OrderedMap not present in the other OrderedMaps.
        /// </summary>
        /// <param name="args">The OrderedMap array that contains the OrderedMaps to be processed.</param>
        /// <returns>Returns a new OrderedMap that contains the elements of the first OrderedMap not present in the other OrderedMaps.</returns>
        public static OrderedMap Difference(params OrderedMap[] args)
        {
            OrderedMap newOrderedMap = args[0];
            for (int index = 1; index < args.Length; index++)
            {
                newOrderedMap = Difference(newOrderedMap, args[index]);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the difference of two OrderedMaps with extra key checking.
        /// That is, elements and keys of the first OrderedMap not present in the second OrderedMap.
        /// </summary>
        /// <param name="input1">The OrderedMap that contains the entries to be searched.</param>
        /// <param name="input2">The OrderedMap where the entries of the first element will be searched.</param>
        /// <returns>Returns a new OrderedMap that contains the entries of the first OrderedMap not present in the second OrderedMap.</returns>
        public static OrderedMap DifferenceWithKey(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;
            for (int index = 0; index < input1.Count; index++)
            {
                System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
                if (input2.SearchStringRepresentation(entry) == -1)
                {
                    if (newOrderedMap == null) newOrderedMap = new OrderedMap();
                    newOrderedMap[entry.Key] = entry.Value;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the difference of two or more OrderedMaps with extra key checking.
        /// That is, elements and keys of the first OrderedMap not present in the other OrderedMaps.
        /// </summary>
        /// <param name="args">The OrderedMap array that contains the OrderedMaps to be processed.</param>
        /// <returns>Returns a new OrderedMap that contains the entries of the first OrderedMap not present in the other OrderedMaps.</returns>
        public static OrderedMap DifferenceWithKey(params OrderedMap[] args)
        {
            OrderedMap newOrderedMap = args[0];
            for (int index = 1; index < args.Length; index++)
            {
                newOrderedMap = DifferenceWithKey(newOrderedMap, args[index]);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Creates a new OrderedMap and fills it with the specified value.
        /// </summary>
        /// <param name="startIndex">The index used to calculate the keys.</param>
        /// <param name="num">The number of entries of the value.</param>
        /// <param name="theValue">The value to fill the OrderedMap with.</param>
        /// <returns>Returns a new OrderedMap filled with the specified value.</returns>
        public static OrderedMap Fill(int startIndex, int num, object theValue)
        {
            OrderedMap newOrderedMap = new OrderedMap();
            for (int index = startIndex; index < startIndex + num; index++)
                newOrderedMap[index] = theValue;
            return newOrderedMap;
        }

        /// <summary>
        /// Filters the elements of the specified OrderedMap using the specified method that belongs to the specified instance.
        /// </summary>
        /// <param name="input">The Ordered to be filtered.</param>
        /// <param name="functionName">The callback method used to filter the OrderedMap.</param>
        /// <param name="instance">The instance which defines the callback method.</param>
        /// <returns>Returns a new OrderedMap with the filtered elements.</returns>
        public static OrderedMap Filter(OrderedMap input, string methodName, object instance)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                if (methodName == null)
                {
                    //No filter is needed.
                    newOrderedMap = new OrderedMap(input, false);
                }
                else
                {
                    try
                    {
                        newOrderedMap = new OrderedMap();
                        System.Type theType = instance.GetType();
                        System.Reflection.MethodInfo callbackMethod = theType.GetMethod(methodName);

                        foreach (string theKey in input.Keys)
                        {
                            if ((bool) callbackMethod.Invoke(instance, new object[] {input[theKey]}))
                                newOrderedMap[theKey] = input[theKey];
                        }
                    }
                    catch (System.Exception exception)
                    {
                        throw exception;
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Exchanges all keys in the OrderedMap with their associated values.
        /// </summary>
        /// <param name="toTransform">The OrderedMap that contains the keys and values to be exchanged.</param>
        /// <returns>Returns a new exchanged OrderedMap.</returns>
        public static OrderedMap Flip(OrderedMap toTransform)
        {
            OrderedMap newOrderedMap = null;

            if (toTransform != null && toTransform.Count > 0)
            {
                try
                {
                    newOrderedMap = new OrderedMap();
                    foreach (string key in toTransform.Keys)
                    {
                        newOrderedMap[toTransform[key]] = key;
                    }
                }
                catch (System.Exception exception)
                {
                    throw exception;
                }
            }

            return newOrderedMap;
        }

        /// <summary>
        /// Computes the intersection of two OrderedMaps. That is, elements of the first OrderedMap present in the second OrderedMap.
        /// </summary>
        /// <param name="input1">The OrderedMap that contains the elements to be searched.</param>
        /// <param name="input2">The OrderedMap where the elements of the first element will be searched.</param>
        /// <returns>Returns a new OrderedMap that contains the elements of the first OrderedMap present in the second OrderedMap.</returns>
        public static OrderedMap Insersection(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;
            for (int index = 0; index < input1.Count; index++)
            {
                System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
                if (input2.SearchStringRepresentation(entry.Value.ToString()) != -1)
                {
                    if (newOrderedMap == null) newOrderedMap = new OrderedMap();
                    newOrderedMap[entry.Key] = entry.Value;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the intersection of two or more OrderedMaps. That is, elements of the first OrderedMap present in the other OrderedMaps.
        /// </summary>
        /// <param name="args">The OrderedMap array that contains the OrderedMaps to be processed.</param>
        /// <returns>Returns a new OrderedMap that contains the elements of the first OrderedMap present in the other OrderedMaps.</returns>
        public static OrderedMap Intersection(params OrderedMap[] args)
        {
            OrderedMap newOrderedMap = args[0];
            for (int index = 1; index < args.Length; index++)
            {
                newOrderedMap = Insersection(newOrderedMap, args[index]);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the intersection of two OrderedMaps with extra key checking.
        /// That is, elements and keys of the first OrderedMap present in the second OrderedMap.
        /// </summary>
        /// <param name="input1">The OrderedMap that contains the entries to be searched.</param>
        /// <param name="input2">The OrderedMap where the entries of the first element will be searched.</param>
        /// <returns>Returns a new OrderedMap that contains the entries of the first OrderedMap present in the second OrderedMap.</returns>
        public static OrderedMap IntersectionWithKey(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;
            for (int index = 0; index < input1.Count; index++)
            {
                System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
                if (input2.SearchStringRepresentation(entry) != -1)
                {
                    if (newOrderedMap == null) newOrderedMap = new OrderedMap();
                    newOrderedMap[entry.Key] = entry.Value;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Computes the intersection of two or more OrderedMaps with extra key checking.
        /// That is, elements and keys of the first OrderedMap present in the other OrderedMaps.
        /// </summary>
        /// <param name="args">The OrderedMap array that contains the OrderedMaps to be processed.</param>
        /// <returns>Returns a new OrderedMap that contains the entries of the first OrderedMap present in the other OrderedMaps.</returns>
        public static OrderedMap IntersectionWithKey(params OrderedMap[] args)
        {
            OrderedMap newOrderedMap = args[0];
            for (int index = 1; index < args.Length; index++)
            {
                newOrderedMap = IntersectionWithKey(newOrderedMap, args[index]);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Checks if the specified key is in a standard representation of an integer.
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        /// <returns>Returns true if the key is a standard representation of an integer, otherwise returns false.</returns>
        public static bool IsKeyInteger(string key)
        {
            //it seems the help file is not synchronized with the real php implementation. 
            //Negative numbers are not being considered as integer keys.
            return System.Text.RegularExpressions.Regex.IsMatch(key, @"^(0|[1-9](\d)*)$");
        }

        /// <summary>
        /// Merges the contents of the first OrderedMap with the contents of the second OrderedMap into a new OrderedMap. 
        /// </summary>
        /// <param name="input1">The first OrderedMap to be merged.</param>
        /// <param name="input2">The second OrderedMap to be merged.</param>
        /// <returns>Returns a new OrderedMap with the merged OrderedMaps.</returns>
        public static OrderedMap Merge(OrderedMap input1, OrderedMap input2)
        {
            OrderedMap newOrderedMap = null;

            //if at least one of the OrderedMaps is not null
            if (input1 != null || input2 != null)
            {
                newOrderedMap = new OrderedMap();
                if (input1 != null)
                {
                    foreach (string key in input1.Keys)
                    {
                        if (IsKeyInteger(key))
                            newOrderedMap[null] = input1[key];
                        else
                            newOrderedMap[key] = input1[key];
                    }
                }

                if (input2 != null)
                {
                    foreach (string key in input2.Keys)
                    {
                        if (IsKeyInteger(key))
                            newOrderedMap[null] = input2[key];
                        else
                            newOrderedMap[key] = input2[key];
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Merges the contents of the specified OrderedMaps. 
        /// </summary>
        /// <param name="args">The OrderedMap array that contains the OrderedMaps to be merged.</param>
        /// <returns>Returns a new OrderedMap with the merged OrderedMaps.</returns>
        public static OrderedMap Merge(params OrderedMap[] args)
        {
            OrderedMap newOrderedMap = args[0];
            for (int index = 1; index < args.Length; index++)
            {
                newOrderedMap = Merge(newOrderedMap, args[index]);
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Pads de specified OrderedMap with the specified pad value. 
        /// If <code>padSize</code> is negative, then the OrderedMap will be left-padded, otherwise it will be right-paddded.
        /// </summary>
        /// <param name="input">The OrderedMap that will be use to do the padding.</param>
        /// <param name="padSize">The padding size.</param>
        /// <param name="padValue">The value used to pad.</param>
        /// <returns>Returns a new paddde OrderedMap.</returns>
        public static OrderedMap Pad(OrderedMap input, int padSize, object padValue)
        {
            OrderedMap newOrderedMap = null;
            if (input != null && input.Count > 0)
            {
                int absSize = System.Math.Abs(padSize);
                if (absSize > input.Count)
                {
                    int toPad = absSize - input.Count;
                    if (padSize > 0)
                    {
                        newOrderedMap = new OrderedMap();
                        foreach (string key in input.Keys)
                        {
                            if (IsKeyInteger(key))
                                newOrderedMap[null] = input[key];
                            else
                                newOrderedMap[key] = input[key];
                        }

                        for (int index = 0; index < toPad; index++)
                        {
                            newOrderedMap[null] = padValue;
                        }
                    }
                    else
                    {
                        newOrderedMap = new OrderedMap();
                        for (int index = 0; index < toPad; index++)
                        {
                            newOrderedMap[null] = padValue;
                        }
                        newOrderedMap = OrderedMap.Merge(newOrderedMap, input);
                    }
                }
                else
                {
                    newOrderedMap = new OrderedMap(input, false);
                }
            }

            return newOrderedMap;
        }

        /// <summary>
        /// Reduces the specified OrderedMap to a single value using a callback function.
        /// </summary>
        /// <param name="input">The OrderedMap that contains the values to be reduced.</param>
        /// <param name="methodName">The method name to use for reduction.</param>
        /// <param name="initial">The initial value of the resulting reduced value.</param>
        /// <param name="instance">The instance that contains the definition of the method used for reduction.</param>
        /// <returns>Returns a single value which represents the reduction of the OrderedMap according to the specified callback method.</returns>
        public static double Reduce(OrderedMap input, string methodName, int initial, object instance)
        {
            double result = initial;

            if (input != null && input.Count > 0)
            {
                try
                {
                    System.Type theType = instance.GetType();
                    System.Reflection.MethodInfo callbackMethod = theType.GetMethod(methodName);

                    if (input != null && input.Count > 0)
                    {
                        object[] parameters = new object[] {initial, input.GetValueAt(0)};
                        result = (double) callbackMethod.Invoke(instance, parameters);
                        for (int index = 1; index < input.Count; index++)
                        {
                            object theValue = input.GetValueAt(index);
                            if (System.Text.RegularExpressions.Regex.IsMatch(theValue.ToString(),
                                                                             OrderedMap._NUMBERREGULAREXPRESSION))
                            {
                                parameters[0] = result;
                                parameters[1] = theValue;
                                result = (double) callbackMethod.Invoke(instance, parameters);
                            }
                        }
                    }
                }
                catch (System.Exception exception)
                {
                    throw exception;
                }
            }

            return result;
        }

        /// <summary>
        /// Reverses the specified OrderedMap, if <code>preserveKeys</code> is true, the original keys will be used.
        /// </summary>
        /// <param name="input">The OrderedMap that will be reversed.</param>
        /// <param name="preserveKeys">The boolean value that indicates whether to preserve the original keys or auto-generate new keys.</param>
        /// <returns>Returns a new reversed OrderedMap.</returns>
        public static OrderedMap Reverse(OrderedMap input, bool preserveKeys)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                newOrderedMap = new OrderedMap();
                for (int index = input.Count - 1; index >= 0; index--)
                {
                    System.Collections.DictionaryEntry entry = input.GetEntryAt(index);
                    if (preserveKeys)
                        newOrderedMap[entry.Key] = entry.Value;
                    else
                    {
                        if (IsKeyInteger(entry.Key.ToString()))
                            newOrderedMap[null] = entry.Value;
                        else
                            newOrderedMap[entry.Key] = entry.Value;
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Shifts (removes) the first element of the specified OrderedMap and returns the value. All the numeric keys are recalculated.
        /// </summary>
        /// <param name="input">The OrderedMap that will be shifted.</param>
        /// <returns>Returns the element that was removed from the specified OrderedMap.</returns>
        public static object Shift(ref OrderedMap input)
        {
            object result = null;
            if (input.Count > 0)
            {
                OrderedMap newOrderedMap = new OrderedMap();
                result = input.GetValueAt(0);
                input.RemoveAt(0);
                foreach (string key in input.Keys)
                {
                    if (OrderedMap.IsKeyInteger(key))
                        newOrderedMap[null] = input[key];
                    else
                        newOrderedMap[key] = input[key];
                }
                input = newOrderedMap;
                input.Reset();
            }
            return result;
        }

        /// <summary>
        /// Randomizes the order of the elements in the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap to be randomly re-ordered.</param>
        public static void Shuffle(ref OrderedMap input)
        {
            OrderedMap.SortValueUser(ref input, "RamdomCompare", input);
        }

        /// <summary>
        /// Returns a new OrderedMap that represents a slice of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap to extract the slice from.</param>
        /// <param name="offset">The starting position of the slice.</param>
        /// <returns>Returns a new OrderedMap that represents a slice of the specified OrderedMap.</returns>
        public static OrderedMap Slice(OrderedMap input, int offset)
        {
            return OrderedMap.Slice(input, offset, input.Count);
        }

        /// <summary>
        /// Returns a new OrderedMap that represents a slice of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap to extract the slice from.</param>
        /// <param name="offset">The starting position of the slice.</param>
        /// <param name="length">The length of the slice.</param>
        /// <returns>Returns a new OrderedMap that represents a slice of the specified OrderedMap.</returns>
        public static OrderedMap Slice(OrderedMap input, int offset, int length)
        {
            OrderedMap newOrderedMap = null;

            if (input != null && input.Count > 0)
            {
                newOrderedMap = new OrderedMap();

                int theOffset = offset, theLength = length;
                _GetAbsPositions(ref theOffset, ref theLength, input.Count);
                for (int index = theOffset; index < theOffset + theLength; index++)
                {
                    System.Collections.DictionaryEntry entry = input.GetEntryAt(index);

                    if (OrderedMap.IsKeyInteger((string) entry.Key))
                        newOrderedMap[null] = entry.Value;
                    else
                        newOrderedMap[entry.Key] = entry.Value;
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Sorts the specified OrderedMap according to the rest of the arguments.
        /// The basic functionality of the sorting mechanism is:
        /// 1. An ArrayList is created using the specified OrderedMap, each ArrayList element is an OrderedMapSortItem (that represent an entry of the OrderedMap).
        /// 2. The method ArrayList.Sort of the just created ArrayList is called.
        /// 3. Since the OrderedMapSortItem class implements the System.IComparable interface, the ArrayList is sorted using that interface of each element.
        /// 4. An sorted-OrderedMap is then created and returned.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        /// <param name="preserveKeys">The boolean value that indicates whether to preserve the original keys (true) or not (false).</param>
        /// <param name="byValue">The value that indicates whether to sort the item by its value (true) or by its key (false).</param>
        /// <param name="methodName">The name of the user-defined method that will be used as comparing method.</param>
        /// <param name="instance">The instance of the class that defines the user-defined method that will be used as comparing method.</param>
        /// <remarks>If predefined sorting mechanisms need to be used (that is SORTREGULAR, SORTNUMERIC, SORTSTRING), then
        ///  the parameters <code>methodName</code> and <code>instance</code> can be null.</remarks>
        public static void Sort(ref OrderedMap input, int sortFlags, bool preserveKeys, bool byValue, string methodName,
                                object instance)
        {
            System.Collections.ArrayList sortedOrderedMap = new System.Collections.ArrayList();
            foreach (string key in input.Keys)
            {
                OrderedMapSortItem item = null;
                if ((methodName == null || methodName == "") && instance == null)
                    item = new OrderedMapSortItem(key, input[key], sortFlags, byValue);
                else
                    item = new OrderedMapSortItem(key, input[key], byValue, methodName, instance);

                sortedOrderedMap.Add(item);
            }

            sortedOrderedMap.Sort();

            OrderedMap newOrderedMap = new OrderedMap();
            for (int index = 0; index < sortedOrderedMap.Count; index++)
            {
                OrderedMapSortItem entry = (OrderedMapSortItem) sortedOrderedMap[index];
                if (preserveKeys)
                    newOrderedMap[entry.Key] = entry.Value;
                else
                    newOrderedMap[null] = entry.Value;
            }

            newOrderedMap._index = input._index;
            input = newOrderedMap;
        }

        /// <summary>
        /// Sorts by key the specified OrderedMap. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortKeyPreserve(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, true, false, null, null);
        }

        /// <summary>
        /// Sorts by key the specified OrderedMap and reverses it. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortKeyPreserveReverse(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, true, false, null, null);
            input = OrderedMap.Reverse(input, true);
        }

        /// <summary>
        /// Sorts by key the specified OrderedMap using a user-defined method. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="methodName">The method name that will be used as comparing method.</param>
        /// <param name="instance">The instance that defined the specified method.</param>
        public static void SortKeyPreserveUser(ref OrderedMap input, string methodName, object instance)
        {
            OrderedMap.Sort(ref input, 0, true, false, methodName, instance);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap. New keys are generated to the values.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortValue(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, false, true, null, null);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortValuePreserve(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, true, true, null, null);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap and reverses it. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortValuePreserveReverse(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, true, true, null, null);
            input = OrderedMap.Reverse(input, true);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap using a user-defined method. The existing keys are preserved.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="methodName">The method name that will be used as comparing method.</param>
        /// <param name="instance">The instance that defined the specified method.</param>
        public static void SortValuePreserveUser(ref OrderedMap input, string methodName, object instance)
        {
            OrderedMap.Sort(ref input, 0, true, true, methodName, instance);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap and reverses it. New keys are generated to the values.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="sortFlags">The sorting options (SORTREGULAR, SORTNUMERIC, SORTSTRING).</param>
        public static void SortValueReverse(ref OrderedMap input, int sortFlags)
        {
            OrderedMap.Sort(ref input, sortFlags, false, true, null, null);
            input = OrderedMap.Reverse(input, false);
        }

        /// <summary>
        /// Sorts by value the specified OrderedMap using a user-defined method. New keys are generated to the values.
        /// </summary>
        /// <param name="input">The OrderedMap to be sorted.</param>
        /// <param name="methodName">The method name that will be used as comparing method.</param>
        /// <param name="instance">The instance that defined the specified method.</param>
        public static void SortValueUser(ref OrderedMap input, string methodName, object instance)
        {
            OrderedMap.Sort(ref input, 0, false, true, methodName, instance);
        }

        /// <summary>
        /// Removes a portion of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap used to remove and replace the specified portion. The changes will be reflected in this OrderedMap.</param>
        /// <param name="offset">The starting position of the portion to be removed.</param>
        /// <returns>Returns a OrderedMap that contains the portion that was removed.</returns>
        public static OrderedMap Splice(ref OrderedMap input, int offset)
        {
            return OrderedMap.Splice(ref input, offset, input.Count, null);
        }

        /// <summary>
        /// Removes a portion of the specified OrderedMap and optionally replaces with the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap used to remove and replace the specified portion. The changes will be reflected in this OrderedMap.</param>
        /// <param name="offset">The starting position of the portion to be removed.</param>
        /// <param name="length">The length of the portion to be removed.</param>
        /// <param name="replacement">The OrderedMap (or the string) used to make the replacement.</param>
        /// <returns>Returns a OrderedMap that contains the portion that was removed.</returns>
        public static OrderedMap Splice(ref OrderedMap input, int offset, int length, object replacement)
        {
            OrderedMap result = null;
            if (input != null && input.Count > 0)
            {
                OrderedMap theReplacement = replacement == null
                                                ? new OrderedMap()
                                                : (replacement is OrderedMap
                                                       ? (OrderedMap) replacement
                                                       : new OrderedMap(replacement));
                int theOffset = offset, theLength = length;
                _GetAbsPositions(ref theOffset, ref theLength, input.Count);
                OrderedMap firstPart = Slice(input, 0, theOffset);
                result = Slice(input, theOffset, theLength);
                OrderedMap lastPart = Slice(input, theOffset + theLength, input.Count);
                OrderedMap newOrderedMap = Merge(Merge(firstPart, theReplacement), lastPart);
                input = newOrderedMap;
            }
            return result;
        }

        /// <summary>
        /// Returns a new OrderedMap that contains the unique values of the specified OrderedMap. In other words, duplicated values are removed.
        /// Two elements are considered equals when the string representation of the elements is the same.
        /// </summary>
        /// <param name="input">The OrderedMap that contains the values to be checked.</param>
        /// <returns>Returns a new OrderedMap that contains the unique values of the specified OrderedMap.</returns>
        public static OrderedMap Unique(OrderedMap input)
        {
            OrderedMap newOrderedMap = null;
            if (input != null)
            {
                newOrderedMap = new OrderedMap();
                OrderedMap valuesOrderedMap = new OrderedMap();
                foreach (string key in input)
                {
                    string theValueString = input[key].ToString();
                    if (valuesOrderedMap[theValueString] == null)
                    {
                        newOrderedMap[key] = input[key];
                        valuesOrderedMap[theValueString] = theValueString;
                    }
                }
            }
            return newOrderedMap;
        }

        /// <summary>
        /// Adds the specified elements to the beggining of the specified OrderedMap.
        /// </summary>
        /// <param name="input">The OrderedMap to add the specified elements.</param>
        /// <param name="args">The elements to be added to the OrderedMap.</param>
        /// <returns>Returns the new number of elements in the OrderedMap.</returns>
        public static int Unshift(ref OrderedMap input, params object[] args)
        {
            OrderedMap newOrderedMap = null;
            if (input != null || args != null)
            {
                newOrderedMap = new OrderedMap();
                if (args != null)
                {
                    for (int index = 0; index < args.Length; index++)
                        newOrderedMap[null] = args[index];
                }

                if (input != null)
                {
                    foreach (string key in input.Keys)
                    {
                        if (OrderedMap.IsKeyInteger(key))
                            newOrderedMap[null] = input[key];
                        else
                            newOrderedMap[key] = input[key];
                    }
                }
            }
            input = newOrderedMap;
            return newOrderedMap.Count;
        }

        #endregion

    }
}