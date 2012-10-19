//
// In order to convert some functionality to Visual C#, the PHP Language Conversion Assistant
// creates "support classes" that duplicate the original functionality.  
//
// Support classes replicate the functionality of the original code, but in some cases they are 
// substantially different architecturally. Although every effort is made to preserve the 
// original architecture of the application in the converted project, the user should be aware that 
// the primary goal of these support classes is to replicate functionality, and that at times 
// the architecture of the resulting solution may differ somewhat.
//

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Contains conversion support elements such as classes, interfaces and static methods.
/// </summary>
namespace PHP
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
			public OrderedMapSortItem(string entryKey, object entryValue, bool byValue, string methodName, object instance)
			{
				this._key = entryKey;
				this._value = entryValue;
				this._sortFlags = 0;
				this._byValue = byValue;
				this._methodName = methodName ;
				this._instance = instance;
			}

			/// <summary>
			/// The key of the element in the original OrderedMap.
			/// </summary>
			public string Key
			{
				get
				{
					return this._key;
				}
			}

			/// <summary>
			/// The value of the element in the original OrderedMap.
			/// </summary>
			public object Value
			{
				get
				{
					return this._value;
				}
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
					bool matchValue1 = System.Text.RegularExpressions.Regex.IsMatch(value1String, OrderedMap._NUMBERREGULAREXPRESSION);
					bool matchValue2 = System.Text.RegularExpressions.Regex.IsMatch(value2String, OrderedMap._NUMBERREGULAREXPRESSION);

					//predefined comparing mechanisms.
					switch (this._sortFlags)
					{
						case OrderedMap.SORTREGULAR:	//Sort regular
							if (matchValue1 && matchValue2) 
								goto case OrderedMap.SORTNUMERIC;
							else if (value1 is OrderedMap)
							{
								return 1;
							}
							else if (value1 is bool)
							{
								if ((bool)value1)
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
								if ((bool)value2)
									return -1;
								else
									return 1;
							}
							else if (matchValue1)
								return 1;
							else if (matchValue2)
								return -1;
							else goto default;

						case OrderedMap.SORTNUMERIC:	//Sort numeric
							//if elements are not numbers, it is the same as zero.
							double value1Double = (matchValue1) ? System.Convert.ToDouble(value1) : 0;
							double value2Double = (matchValue2) ? System.Convert.ToDouble(value2) : 0;

							if (value1Double < value2Double)
								return -1;
							else if (value1Double > value2Double)
								return 1;
							else
								return 0;

						default:	//Sort string
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
						parameters = new object[]{value1, value2};
					else
					{
						parameters = new object[]{OrderedMap.IsKeyInteger((string)value1) ? System.Convert.ToInt32(value1) : value1,
													 OrderedMap.IsKeyInteger((string)value2) ? System.Convert.ToInt32(value2) : value2};
					}

					return (int)callbackMethod.Invoke(this._instance, parameters);
				}
			}
		}

		#endregion

		#region Class Constructors

		/// <summary>
		/// Creates a new empty OrderedMap.
		/// </summary>
		public OrderedMap():base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
		{
			this._index = 0;
		}

		/// <summary>
		/// Adds the values from the ICollection into the new OrderedMap.
		/// </summary>
		/// <param name="collection">The ICollection that contains the values.</param>
		/// <param name="isReadOnly">A boolean value that indicates whether the new instance is read-only.</param>
		public OrderedMap(System.Collections.ICollection collection, bool isReadOnly):base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
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
		public OrderedMap(System.Collections.IDictionary dictionary, bool isReadOnly):base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
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
		public OrderedMap(System.Collections.Specialized.NameValueCollection collection, bool isReadOnly):base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
		{
			for(int index=0; index<collection.Count; index++)
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
		public OrderedMap(OrderedMap orderedMap, bool isReadOnly):base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
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
		public OrderedMap(params object[] args):base(null, new System.Collections.Comparer(System.Globalization.CultureInfo.CurrentCulture))
		{
			int keyIndex = 0, maxIndex = int.MinValue;
			bool useMaxIndex = false;
			for (int index=0; index<args.Length; index++)
			{
				if (args[index] is object[])
				{
					object theKey = ((object[])args[index])[0];
					object theValue = ((object[])args[index])[1];
					if (OrderedMap.IsKeyInteger(theKey.ToString()))
					{
						int intKey = theKey is int ? (int)theKey : System.Convert.ToInt32(theKey);
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
			get  
			{
				return(this.BaseGet(key));
			}
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
			get  
			{
				return(this.BaseGet(key.ToString()));
			}
			set
			{
				this.BaseSet(key.ToString(), value);
			}
		}

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		public object this[object key]
		{
			get  
			{
				return(this.BaseGet(key.ToString()));
			}
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
			get
			{
				return (this._index >= this.Count) || (this._index < 0) ? false : this.GetValueAt(this._index);
			}
		}

		/// <summary>
		/// Gets a value indicating if this OrderedMap contains keys that are not null.
		/// </summary>
		public bool HasKeys  
		{
			get  
			{
				return(this.BaseHasKeys());
			}
		}

		/// <summary>
		/// Gets a string array that contains all the keys in this OrderedMap.
		/// </summary>
		public new string[] Keys
		{
			get
			{
				return(this.BaseGetAllKeys());
			}
		}

		/// <summary>
		/// Gets an object array that contains all the values in this OrderedMap.
		/// </summary>
		public object[] Values
		{
			get
			{
				return(this.BaseGetAllValues());
			}
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
			for (int index = this.Count-1; index >= 0; index--)
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
			if (stringValue.Length < index+1)
			{
				int oldLenght = stringValue.Length;
				stringValue.Length = index+1;
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
					new object[]{1, entry.Value}, 
					new object[]{"value", entry.Value},
					new object[]{0, entry.Key},
					new object[]{"key", entry.Key});

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
			this._index = this.Count-1;
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
			return(entry);
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
				foreach(string theKey in this.Keys)
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
				int randKeyIndex = random.Next(this.Count-1);
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
				for(int index=0; index<numKeys; index++)
				{
					int randKeyIndex = random.Next(this.Count-1);
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
				theValue = this.GetValueAt(this.Count-1);
				this.RemoveAt(this.Count-1);
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
			for(int index=0; index<args.Length; index++)
			{
				this[null] = args[index];
			}
			return this.Count;
		}

		/// <summary>
		/// This method is used to randomly re-order the elements of a OrderedMap. It returns a random number (-1, 0, 1).
		/// </summary>
		/// <param name="dummy1">Unused parameter, it exists only to comply with sorting methods requirements.</param>
		/// <param name="dummy1">Unused parameter, it exists only to comply with sorting methods requirements.</param>
		/// <returns>Returns a random number between -1 a 1 (-1, 0, 1).</returns>
		public int RamdomCompare(object dummy1, object dummy2)
		{
			int compare = (this._random.Next(0, 3)) - 1;
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
			for(int index=0; index<this.Count; index++)
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
			for(int index=0; index<this.Count; index++)
			{
				System.Collections.DictionaryEntry entry = this.GetEntryAt(index);
				if (entry.Value.ToString().Equals(toSearch.Value.ToString()) && entry.Key.ToString().Equals(toSearch.Key.ToString()))
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
			foreach(string key in this.Keys)
			{
				object currentValue = this[key];

				if (currentValue.GetType() == theValue.GetType())	//type checking
				{
					//the operand == is type-dependant, for that reason castings are made according to the values types.
					bool equals = false;
					//for predefined value types, the equality operator (==) returns true if the values of its operands are equal, false otherwise.
					if (currentValue is bool)
						equals = (bool)currentValue == (bool)theValue;
					else if (currentValue is int || currentValue is float || currentValue is double)
						equals = (int)currentValue == (int)theValue;
					else if (currentValue is string)
						//for the string type, == compares the values of the strings.
						equals = (string)currentValue == (string)theValue;
					else if (currentValue is OrderedMap)
						//for the OrderedMap type, the values of the OrderedMaps are compared.
						equals = ((OrderedMap)currentValue).ToStringContents() == ((OrderedMap)theValue).ToStringContents();
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
				foreach(object theValue in this.Values)
				{
					if (System.Text.RegularExpressions.Regex.IsMatch(theValue.ToString(), OrderedMap._NUMBERREGULAREXPRESSION))
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
			string indentSpaces = new string(' ', (indentLevel-1) * 4);
			string toStringContents = "Array\r\n" + indentSpaces + "(\r\n";
			foreach(string theKey in this.Keys)
			{
				object theValue = this[theKey];
				indentSpaces = new string(' ', indentLevel * 4);
				if (theValue is OrderedMap)
				{
					indentLevel++;
					toStringContents = toStringContents + indentSpaces + "[" + theKey + "] => " + ((OrderedMap)theValue).ToStringContents(ref indentLevel);
					indentLevel--;
				}
				else
				{
					string theValueString = theValue is bool ? (bool)theValue ? "1":"" : theValue.ToString();
					toStringContents = toStringContents + indentSpaces + "[" + theKey + "] => " + theValueString + "\r\n";
				}
			}
			indentSpaces = new string(' ', (indentLevel-1) * 4);
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
		
					foreach(string theKey in this.Keys)
					{
						object[] parameters = data == null ? new object[]{this[theKey], theKey} : new object[]{this[theKey], theKey, data};
						callbackMethod.Invoke(instance, parameters);
					}
					result = true;
				}
				catch(System.Exception exception)
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
			if (args.Length == 2) {this[args[1]] = args[0]; return;}

			//if there are at least two indexers go on.

			string theKey = "";
			//loop over the specified keys creating new OrderedMaps (i.e. dimensions) when necessary.
			OrderedMap currentOrderedMap = this; 
			for(int index=1; index<args.Length-1; index++)
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
						if (index == args.Length-1)
						{
							//the string value should be changed by the new value.
							newValue = args[0];
						}
						else if (index == args.Length-2)
						{
							newValue = this._ReplaceCharAt((string)currentOrderedMap[theKey], (int)args[index+1], args[0].ToString()[0]);
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
				currentOrderedMap = (OrderedMap)currentOrderedMap[theKey];
			} //end for

			//current OrderedMap represents the last dimension, set a new entry in the current OrderedMap,
			//formed by the last key (if any) and the specified value.
			currentOrderedMap[args[args.Length-1]] = args[0];
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
			for(int index=0; index<args.Length-1; index++)
			{
				if (currentOrderedMap[args[index]] != null)
				{
					if (currentOrderedMap[args[index]] is string)	
					{
						//current element is not a OrderedMap but a string, if possible return the substring.
						if (index == args.Length-2)
						{
							//arr[0][1]: where arr[0] is a string, the second indexer (1) is actually a string indexer.
							return ((string)currentOrderedMap[args[index]]).Substring((int)args[index+1], 1);
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
						currentOrderedMap = (OrderedMap)currentOrderedMap[args[index]];
					}
				}
				else
				{
					//specified element does not exist in OrderedMap
					return string.Empty;
				}
			}
			//current OrderedMap represents the last dimension, return the value associated to the last key.
			return currentOrderedMap[args[args.Length-1]];
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
			theLength = theLength+theOffset > count ? count - theOffset : theLength;

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
					foreach(string key in input2.Keys)
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
				foreach(string key in input.Keys)
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
				int totalChunks = input.Count / size;
				totalChunks += (input.Count % size) > 0 ? 1 : 0;
				for(int index = 0; index<totalChunks; index++)
				{
					OrderedMap theChunk = new OrderedMap();
					for(int chunkIndex=0; chunkIndex<size; chunkIndex++)
					{
						if ((index * size) + chunkIndex < input.Count)
						{
							string key = "";
							if (preserveKeys)
								key = input.GetKeyAt((index * size) + chunkIndex);
							else
								key = chunkIndex.ToString();

							theChunk[key] = input.GetValueAt((index * size) + chunkIndex);
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
                    count = ((OrderedMap)variable).Count;
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
				foreach(object theValue in input.Values)
				{
					if (newOrderedMap[theValue] != null)
						newOrderedMap[theValue] = ((int)newOrderedMap[theValue]) + 1;
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
			
			for(int index = low; increment ? index<=high : index>=high; index += increment ? 1 : -1)
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
			int lowCharInt = (int)lowChar[0], highCharInt = (int)highChar[0];
			bool increment =  highCharInt >= lowCharInt ? true : false; 
			
			for(int index = lowCharInt; increment ? index<=highCharInt : index>=highCharInt; index += increment ? 1 : -1)
			{
				newOrderedMap[null] = new string(new char[]{(char)index});
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
			for(int index=0; index<input1.Count; index++)
			{
				System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
				if (input2.SearchStringRepresentation(entry.Value.ToString()) == -1)
				{
					if (newOrderedMap == null) newOrderedMap =  new OrderedMap();
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
			for(int index=1; index<args.Length; index++)
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
			for(int index=0; index<input1.Count; index++)
			{
				System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
				if (input2.SearchStringRepresentation(entry) == -1)
				{
					if (newOrderedMap == null) newOrderedMap =  new OrderedMap();
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
			for(int index=1; index<args.Length; index++)
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
			for(int index=startIndex; index<startIndex+num; index++)
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
			
						foreach(string theKey in input.Keys)
						{
							if ((bool)callbackMethod.Invoke(instance, new object[]{input[theKey]}))
								newOrderedMap[theKey] = input[theKey];
						}
					}
					catch(System.Exception exception)
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
				catch(System.Exception exception)
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
			for(int index=0; index<input1.Count; index++)
			{
				System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
				if (input2.SearchStringRepresentation(entry.Value.ToString()) != -1)
				{
					if (newOrderedMap == null) newOrderedMap =  new OrderedMap();
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
			for(int index=1; index<args.Length; index++)
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
			for(int index=0; index<input1.Count; index++)
			{
				System.Collections.DictionaryEntry entry = input1.GetEntryAt(index);
				if (input2.SearchStringRepresentation(entry) != -1)
				{
					if (newOrderedMap == null) newOrderedMap =  new OrderedMap();
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
			for(int index=1; index<args.Length; index++)
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
					foreach(string key in input1.Keys)
					{
						if (IsKeyInteger(key))
							newOrderedMap[null] = input1[key];
						else
							newOrderedMap[key] = input1[key];
					}
				}
				
				if (input2 != null)
				{
					foreach(string key in input2.Keys)
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
			for(int index=1; index<args.Length; index++)
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
						foreach(string key in input.Keys)
						{
							if (IsKeyInteger(key))
								newOrderedMap[null] = input[key];
							else
								newOrderedMap[key] = input[key];
						}

						for (int index=0; index<toPad; index++)
						{
							newOrderedMap[null] = padValue;
						}
					}
					else
					{
						newOrderedMap = new OrderedMap();
						for (int index=0; index<toPad; index++)
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
						object[] parameters = new object[]{initial, input.GetValueAt(0)};
						result = (double)callbackMethod.Invoke(instance, parameters);
						for(int index=1; index<input.Count; index++)
						{
							object theValue = input.GetValueAt(index);
							if (System.Text.RegularExpressions.Regex.IsMatch(theValue.ToString(), OrderedMap._NUMBERREGULAREXPRESSION))
							{
								parameters[0] = result;
								parameters[1] = theValue;
								result = (double)callbackMethod.Invoke(instance, parameters);
							}
						}
					}
				}
				catch(System.Exception exception)
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
				for(int index=input.Count-1; index>=0; index--)
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
				foreach(string key in input.Keys)
				{
					if(OrderedMap.IsKeyInteger(key))
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
				for(int index=theOffset; index < theOffset+theLength; index++)
				{
					System.Collections.DictionaryEntry entry = input.GetEntryAt(index);
				
					if(OrderedMap.IsKeyInteger((string)entry.Key))
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
		public static void Sort(ref OrderedMap input, int sortFlags, bool preserveKeys, bool byValue, string methodName, object instance)
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
			for(int index=0; index<sortedOrderedMap.Count; index++)
			{
				OrderedMapSortItem entry = (OrderedMapSortItem)sortedOrderedMap[index];
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
				OrderedMap theReplacement = replacement == null ? new OrderedMap() : (replacement is OrderedMap ? (OrderedMap)replacement : new OrderedMap(replacement));
				int theOffset = offset, theLength = length;
				_GetAbsPositions(ref theOffset, ref theLength, input.Count);
				OrderedMap firstPart = Slice(input, 0, theOffset);
				result = Slice(input, theOffset, theLength);
				OrderedMap lastPart = Slice(input, theOffset+theLength, input.Count);
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
				foreach(string key in input)
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
					for(int index=0; index<args.Length; index++)
						newOrderedMap[null] = args[index];
				}

				if (input != null)
				{
					foreach(string key in input.Keys)
					{
						if(OrderedMap.IsKeyInteger(key))
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
	/*******************************/
	/// <summary>
	/// Provides static methods related to Type casting
	/// </summary>
	public class TypeSupport
	{
		#region ToDouble

		/// <summary>
		/// Converts a string to its numeric double representation.
		/// </summary>
		/// <param name="stringValue">The string value to convert.</param>
		/// <returns>Returns the numeric double representation of the string.</returns>
		public static double ToDouble(string stringValue) 
		{
			double doubleValue = 0;
			if (stringValue != null)
			{
				string regStr = "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?";
				System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(regStr);
				if(reg.IsMatch(stringValue))
				{
					System.Text.RegularExpressions.Match match = reg.Match(stringValue);
					try 
					{
						doubleValue = System.Convert.ToDouble(match.Value);
					} 
					catch 
					{
						doubleValue = 0;
					}
				} 
			}
			return doubleValue;
		}

		/// <summary>
		/// Converts an object to its numeric double representation.
		/// </summary>
		/// <param name="instance">The object value to convert.</param>
		/// <returns>Returns the numeric double representation of the object.</returns>
		public static double ToDouble(object instance)
		{
			double result = 0;
			if (instance != null)
			{
				if (instance is int || instance is long || instance is double || instance is bool)
				{
					result = System.Convert.ToDouble(instance);
				}
				else if (instance is string)
				{
					result = ToDouble((string)instance);
				}
				else if (instance is OrderedMap)
				{
					OrderedMap orderedMap = (OrderedMap)instance;
					result = orderedMap.Count == 0 ? 0 : 1;
				}
				else
				{
					result = 1;
				}
			}
			return result;
		}

		#endregion

		#region ToInt32

		/// <summary>
		/// Converts a string to its numeric integer representation.
		/// </summary>
		/// <param name="stringValue">The string value to convert.</param>
		/// <returns>Returns the numeric integer representation of the string.</returns>
		public static int ToInt32(string stringValue) 
		{
			int intValue = 0;
			if (stringValue != null)
			{
				string regStr = "^[-+]?[0-9]+";
				System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(regStr);
				if(reg.IsMatch(stringValue))
				{
					System.Text.RegularExpressions.Match match = reg.Match(stringValue);
					try 
					{
						intValue = System.Convert.ToInt32(match.Value);
					} 
					catch 
					{
						intValue = 0;
					}
				}
			}
			return intValue;
		}

		/// <summary>
		/// Converts an object to its numeric int representation.
		/// </summary>
		/// <param name="instance">The object value to convert.</param>
		/// <returns>Returns the numeric int representation of the object.</returns>
		public static int ToInt32(object instance)
		{
			int result = 0;
			if (instance != null)
			{
				if (instance is int || instance is long || instance is double || instance is bool)
				{
					result = System.Convert.ToInt32(instance);
				}
				else if (instance is string)
				{
					result = ToInt32((string)instance);
				}
				else if (instance is OrderedMap)
				{
					OrderedMap orderedMap = (OrderedMap)instance;
					result = orderedMap.Count == 0 ? 0 : 1;
				}
				else
				{
					result = 1;
				}
			}

			return result;
		}


		#endregion
	
		#region ToBoolean

		/// <summary>
		/// Obtains a boolean value from a string.
		/// </summary>
		/// <param name="stringValue">The string value to convert.</param>
		/// <returns>If the string is empty or "0" returns false, otherwise returns true.</returns>
		public static bool ToBoolean(string stringValue) 
		{
			bool result = false;
			if (stringValue != null)
			{
				if ((stringValue == "") || (stringValue == "0")) 
				{
					result = false;
				} 
				else 
				{
					result  = true;
				}
			}
			return result;
		}

		/// <summary>
		/// Obtains a boolean value from an object.
		/// </summary>
		/// <param name="objectValue">The object to convert to boolean.</param>
		/// <returns>Returns the boolean representation of the object.</returns>
		public static bool ToBoolean(object objectValue) 
		{
			bool result = false;
			if (objectValue != null)
			{
				if (objectValue is int || objectValue is long || objectValue is double || objectValue is bool)
				{
					result = System.Convert.ToBoolean(objectValue);
				}
				else if (objectValue is string)
				{
					result = ToBoolean((string)objectValue);
				}
				else if (objectValue is OrderedMap)
				{
					OrderedMap orderedMap = (OrderedMap)objectValue;
					result = orderedMap.Count == 0 ? false : true;
				}
				else
				{
					result = true;
				}
			}
			return result;
		}

		#endregion

		#region ToString

		/// <summary>
		/// Obtains a string value from an object.
		/// </summary>
		/// <param name="objectValue">The object to convert to string.</param>
		/// <returns>Returns the string representation of the object.</returns>
		public static string ToString(object objectValue) 
		{
			string result = "";
			if (objectValue != null)
			{
                if (objectValue is System.Text.StringBuilder)
                {
                    result = objectValue.ToString();
                }
                else if (objectValue is double || objectValue is long || objectValue is int || objectValue is string)
                {
                    result = objectValue.ToString();
                }
                else if (objectValue is bool)
                {
                    result = (bool)objectValue ? "1" : "";
                }
                else if (objectValue is OrderedMap)
                {
                    result = "Array";
                }
                else
                {
                    result = "Object";
                }
			}
			return result;
		}

		#endregion
		
		#region ToArray

		/// <summary>
		/// Converts an object into an OrderedMap
		/// </summary>
		/// <param name="obj">The object to convert to OrderedMap.</param>
		/// <returns>Returns an OrderedMap representation of the object.</returns>
		public static OrderedMap ToArray(object obj) 
		{
			OrderedMap result = new OrderedMap();

			if (obj != null)
			{
				if (obj is OrderedMap)
					result = new OrderedMap((OrderedMap)obj, false);
				else if (obj is string || obj is int || obj is long || obj is double || obj is bool)
					result = new OrderedMap(obj);
				else
				{
					System.Reflection.FieldInfo[] fields = obj.GetType().GetFields();
					for(int index=0; index<fields.Length; index++)
						result[fields[index].Name] = fields[index].GetValue(obj);
				}
			}
			return result;
		}

		#endregion
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to miscellaneous functions.
	/// </summary>
	public class MiscSupport
	{
		/// <summary>
		/// Searches (using reflection) for the value of the specified string as a defined constant.
		/// </summary>
		/// <param name="constant">The name of the constant to search.</param>
		/// <param name="mb">The current method, required for reflection.</param>
		/// <returns>Returns the value of the constant, or an empty string if not defined.</returns>
		public static string Constant(string constant, System.Reflection.MethodBase method)
		{
			System.Reflection.FieldInfo field = method.DeclaringType.GetField(constant);
			return ((field == null)? System.String.Empty : field.GetValue(field.GetType()).ToString());
		}

		/// <summary>
		/// Displays the contents of the specified file as HTML code.
		/// </summary>
		/// <param name="filename">The relative path of the file.</param>
		public static void ShowSourceFile(string filename)
		{
			try
			{
				using (System.IO.StreamReader sr = System.IO.File.OpenText(System.Web.HttpContext.Current.Request.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
						System.Web.HttpContext.Current.Response.Write(System.Web.HttpContext.Current.Server.HtmlEncode(line) + "<br>");
				}
			}
			catch
			{
				System.Web.HttpContext.Current.Response.Write(System.Web.HttpContext.Current.Request.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath)  + "/" + filename + ", Failed to Open Stream.");
			}
		}

		/// <summary>
		/// Returns the contents of the specified file as HTML code.
		/// </summary>
		/// <param name="filename">The relative path of the file.</param>
		/// <returns>Returns the contents of the specified file.</returns>
		public static string GetSourceFile(string filename)
		{
			string ret = "";
			try
			{
				using (System.IO.StreamReader sr = System.IO.File.OpenText(System.Web.HttpContext.Current.Request.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath) + "/" + filename))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
						ret += (System.Web.HttpContext.Current.Server.HtmlEncode(line) + "<br>");
				}
			}
			catch
			{
				ret += (System.Web.HttpContext.Current.Request.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath)  + "/" + filename + ", Failed to Open Stream.");
			}
			return ret;
		}

		/// <summary>
		/// Returns an OrderedMap with the browser information the user was expecting.
		/// </summary>
		/// <returns>Returns an OrderedMap with browser information.</returns>
		public static OrderedMap GetBrowserInfo()
		{
			OrderedMap browserInfo = new OrderedMap();
			browserInfo.Add("browser_name_pattern", System.Web.HttpContext.Current.Request.Browser.Browser + System.Web.HttpContext.Current.Request.Browser.Version);
			browserInfo.Add("browser", System.Web.HttpContext.Current.Request.Browser.Browser);
			browserInfo.Add("version", System.Web.HttpContext.Current.Request.Browser.Version);
			browserInfo.Add("majorver", System.Web.HttpContext.Current.Request.Browser.MajorVersion.ToString());
			browserInfo.Add("minorver", System.Web.HttpContext.Current.Request.Browser.MinorVersion.ToString());
			browserInfo.Add("frames", System.Web.HttpContext.Current.Request.Browser.Frames? "1" : "0");
			browserInfo.Add("tables", System.Web.HttpContext.Current.Request.Browser.Tables? "1" : "0");
			browserInfo.Add("cookies", System.Web.HttpContext.Current.Request.Browser.Cookies? "1" : "0");
			browserInfo.Add("backgroundsounds", System.Web.HttpContext.Current.Request.Browser.BackgroundSounds? "1" : "0");
			browserInfo.Add("vbscript", System.Web.HttpContext.Current.Request.Browser.VBScript? "1" : "0");
			browserInfo.Add("javascript", System.Web.HttpContext.Current.Request.Browser.JavaScript? "1" : "0");
			browserInfo.Add("javaapplets", System.Web.HttpContext.Current.Request.Browser.JavaApplets? "1" : "0");
			browserInfo.Add("activexcontrols", System.Web.HttpContext.Current.Request.Browser.ActiveXControls? "1" : "0");
			browserInfo.Add("cdf", System.Web.HttpContext.Current.Request.Browser.CDF? "1" : "0");
			browserInfo.Add("aol", System.Web.HttpContext.Current.Request.Browser.AOL? "1" : "0");
			browserInfo.Add("beta", System.Web.HttpContext.Current.Request.Browser.Beta? "1" : "0");
			browserInfo.Add("win16", System.Web.HttpContext.Current.Request.Browser.Win16? "1" : "0");
			browserInfo.Add("crawler", System.Web.HttpContext.Current.Request.Browser.Crawler? "1" : "0");
			browserInfo.Add("netclr", System.Web.HttpContext.Current.Request.Browser.ClrVersion.ToString());

			return browserInfo;
		}	
		
		/// <summary>
		/// Prints out the specified message and halts the execution of the page.
		/// </summary>
		/// <param name="message">The message to print out.</param>
		/// <returns>Always returns false.</returns>
		/// <remarks>This method is only used when the original 'exitt' function is used in an expression.</remarks>
		public static bool End(string message)
		{
			System.Web.HttpContext.Current.Response.Write(message);
			System.Web.HttpContext.Current.Response.End();
			return false;
		}

		/// <summary>
		/// Halts the execution of the page.
		/// </summary>
		/// <returns>Always returns false.</returns>
		/// <remarks>This method is only used when the original 'exitt' function is used in an expression.</remarks>
		public static bool End()
		{
			System.Web.HttpContext.Current.Response.End();
			return false;
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to math functions.
	/// </summary>
	public class MathSupport
	{
		/// <summary>
		/// Converts a number in decimal base to any base.
		/// </summary>
		/// <param name="number">The number to be converted.</param>
		/// <param name="toBase">The new base. It can be in the range 2-36.</param>
		/// <returns>Returns the string representacion of the number in the new base.</returns>
		public static string DecToAny(double number, int toBase)
		{
			string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			int digitValue;
			string result = "";

			//check base
			if (toBase<2 || toBase>36) 
				throw new System.Exception("Invalid 'to Base'");
			else
			{
				//get the list of valid digits for the given base
				digits = digits.Substring(0,toBase);
					
				//convert to the other base
				while(number>0)
				{
					digitValue = (int)(number % (double)toBase);
					number /= toBase;
					result = digits.Substring(System.Convert.ToInt32(digitValue), 1) + result; 			
				}
				
				return result;
			}		
		}

		/// <summary>
		/// Converts a number in any base to decimalbase.
		/// </summary>
		/// <param name="otherBaseNumber">The string representing the number to be converted.</param>
		/// <param name="fromBase">The base of the nuber to be converted.</param>
		/// <returns>Returns the representation of the number in decimal base.</returns>
		public static int AnyToDec(string otherBaseNumber, int fromBase)
		{			
			string digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
			int digitValue;
			int result =0;

			//check Base
			if (fromBase<2 || fromBase>36) 
				throw new System.Exception("Invalid 'from Base'");
			else
			{
				//get the list of valid digits for the given base
				digits = digits.Substring(0,fromBase);

				otherBaseNumber = otherBaseNumber.ToUpper();

				//convert to decimal
				for (int i = 0; i<otherBaseNumber.Length; i++)
				{
					// get the digit's value
					digitValue = digits.IndexOf(otherBaseNumber.Substring(i,1),0,digits.Length);
					if (digitValue < 0) 
						throw new System.Exception("Invalid 'from Base'");
					else
					{
						//add to result
						result = result * fromBase + digitValue;
					}								
				}
			}
			return result;		
		}

		/// <summary>
		/// Returns a string representing the given number in the 'toBase'.
		/// </summary>
		/// <param name="number">The string represening the number to be converted.</param>
		/// <param name="fromBase">The base of the given number.</param>
		/// <param name="toBase">The new base.</param>
		/// <returns>Returns the string representation of the number.</returns>
		public static string BaseConvert(string number, int fromBase, int toBase)
		{
			return (DecToAny(AnyToDec(number,fromBase),toBase).TrimStart(new char[]{'0'}));
		}

		/// <summary>
		/// Converts an angle in degrees to radians.
		/// </summary>
		/// <param name="angleInDegrees">The double value of angle in degrees to convert.</param>
		/// <returns>Returns the value of the angle in radians.</returns>
		public static double DegreesToRadians(double angleInDegrees)
		{
			double valueRadians =  (2 * System.Math.PI) / 360;
			return angleInDegrees * valueRadians;
		}
			
		/// <summary>
		/// Converts an angle in radians to degrees.
		/// </summary>
		/// <param name="angleInRadians">The double value of angle in radians to convert.</param>
		/// <returns>Returns the value of the angle in degrees.</returns>
		public static double RadiansToDegrees(double angleInRadians)
		{
			double valueDegrees = 360 / (2 * System.Math.PI) ;
			return angleInRadians * valueDegrees;
		}		
			
		/// <summary>
		/// Returns the floating point remainder of dividing x by y.
		/// </summary>
		/// <param name="x">The dividend.</param>
		/// <param name="y">The divisor.</param>
		/// <returns>Returns the floating point remainder of x/y.</returns>
		public static double FMod(double x, double y)
		{
			return x - (System.Math.Floor(x/y) * y);
		}	

		/// <summary>
		/// Returns sqrt(x*x + y*y).
		/// </summary>
		/// <param name="x">The x value.</param>
		/// <param name="y">The y value.</param>
		/// <returns>Returns sqrt(x*x + y*y).</returns>
		public static double Hypot(double x, double y)
		{
			return System.Math.Sqrt(x*x + y*y);
		}	

		private static System.Random rand = new System.Random();

		/// <summary>
		/// Random number generator instance. The seed is initialized the first time MathSupport Class is used.
		/// </summary>
		public static System.Random Rand
		{
			get
			{
				return MathSupport.rand;
			}
		}
			
		/// <summary>
		/// Seeds the random number generator with the give seed.
		/// </summary>
		/// <param name="seed">The new seed.</param>
		public static void SeedRand(double seed)
		{
			MathSupport.rand = new System.Random((int)seed);
		}

		/// <summary>
		/// Returns the maximum or minimun value of the argument list.
		/// </summary>
		/// <param name="returnMax">The value that indicates whether to return the maximum value (true) or the minimum value (false) of the argument list.</param>
		/// <param name="args">The argument list that contains the elements to be tested.</param>
		/// <returns>The maximum or minimum value of the argument list.</returns>
		private static object _MaxMin(bool returnMax, params object[] args)
		{
			object result = null;
			if (args.Length > 0)
			{
				OrderedMap orderedMap = null;
				if (args[0] is OrderedMap)
					orderedMap = (OrderedMap)args[0];
				else
					orderedMap = new OrderedMap(args, false);

				OrderedMap.SortValue(ref orderedMap, OrderedMap.SORTREGULAR);
				int index = returnMax ? orderedMap.Count-1 : 0;
				result = orderedMap.GetValueAt(index);
			}
			return result;
		}

		/// <summary>
		/// Returns the maximum value of the argument list.
		/// </summary>
		/// <param name="args">The argument list that contains the elements to be tested.</param>
		/// <returns>The maximum value of the argument list.</returns>
		public static object Max(params object[] args)
		{
			return _MaxMin(true, args);
		}

		/// <summary>
		/// Returns the minimum value of the argument list.
		/// </summary>
		/// <param name="args">The argument list that contains the elements to be tested.</param>
		/// <returns>The minimum value of the argument list.</returns>
		public static object Min(params object[] args)
		{
			return _MaxMin(false, args);
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods to work with strings.
	/// </summary>
	public class StringSupport
	{
		/// <summary>
		/// Quotes the specified string with backslashes. 
		/// The characters to be quoted are: quote('), double quote("), backslash(\) and null(0).
		/// </summary>
		/// <param name="theString">The string to be quoted with backslashes.</param>
		/// <returns>Returns the quoted string.</returns>
		public static string AddSlashes(string theString)
		{
			string tempStr = "";
			tempStr = theString.Replace("\\", "\\\\");
			tempStr = tempStr.Replace("\'", "\\\'");
			tempStr = tempStr.Replace("\"", "\\\"");
			tempStr = tempStr.Replace("\0", "\\0");
			return tempStr;
		}

		/// <summary>
		/// Un-Quotes the specified string with backslashes.
		/// The characters to be un-quoted are: quote('), double quote("), backslash(\) and null(0).
		/// </summary>
		/// <param name="theString">The string to be un-quoted with backslashes.</param>
		/// <returns>Returns the un-quoted string.</returns>
		public static string RemoveSlashes(string theString)
		{
			string tempStr = "";
			tempStr = theString.Replace("\\\\", "\\");
			tempStr = tempStr.Replace("\\\'", "\'");
			tempStr = tempStr.Replace("\\\"", "\"");
			tempStr = tempStr.Replace("\\0", "\0");
			return tempStr;
		}

		/// <summary>
		/// Converts a string containing binary data to a string with its hexadecimal representation.
		/// </summary>
		/// <param name="binString">The string with binary data to be converted.</param>
		/// <returns>Returns an ASCII string containing the hexadecimal representation of the binary string.</returns>
		public static string BinToHex(string binString)
		{
			string hexString = "";
			foreach (byte theByte in System.Text.Encoding.ASCII.GetBytes(binString))
				hexString += System.Convert.ToString(System.Convert.ToInt32(theByte), 16).PadLeft(2, '0');
			return hexString;
		}

		/// <summary>
		/// Shuffles a string value using the support random seeded instance.
		/// </summary>
		/// <param name="input">The string to shuffle.</param>
		/// <returns>Returns a new shuffled string.</returns>
		public static string StringShuffle(string input)
		{
			System.Text.StringBuilder oldString = new System.Text.StringBuilder(input);
			System.Text.StringBuilder newString = new System.Text.StringBuilder();
			newString.Length = input.Length;

			for (int charIndex=oldString.Length-1; charIndex>=0; charIndex--)
			{
				//Use the support random seeded instance.
				int newIndex = MathSupport.Rand.Next(0, charIndex);
				newString[charIndex] = oldString[newIndex];
				oldString[newIndex] = oldString[charIndex];
			}
			return newString.ToString();
		}

		/// <summary>
		/// Joins the elements specified in the OrderedMap parameter using the supplied separator.
		/// </summary>
		/// <param name="separator">The string that will be used as separator for the concatenated elements.</param>
		/// <param name="array">The OrderedMap which elements will be used to concatenate.</param>
		/// <returns>Returns a string consisting of the elements of the OrderedMap parameter joined by the separator parameter.</returns>
		public static string Join(string separator, OrderedMap array)
		{
			string[] elements = new string[array.Count];

			for (int i=0; i < array.Count; i++)
				elements[i] = array.GetValueAt(i).ToString();
		
			return string.Join(separator, elements);
		}

		/// <summary>
		/// Returns localized numeric and monetary formatting information.
		/// </summary>
		/// <returns>Returns an OrderedMap containing localized numeric and monetary formatting information.</returns>
		public static OrderedMap GetNumberFormatInfo()
		{
			OrderedMap array = new OrderedMap();

			array.Add("decimal_point", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
			array.Add("thousands_sep", System.Globalization.NumberFormatInfo.CurrentInfo.NumberGroupSeparator);
			//array.Add("grouping", );
			//array.Add("int_curr_symbol", );
			array.Add("currency_symbol", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencySymbol);
			array.Add("mon_decimal_point", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator);
			array.Add("mon_thousands_sep", System.Globalization.NumberFormatInfo.CurrentInfo.CurrencyGroupSeparator);
			//array.Add("mon_grouping", );
			array.Add("positive_sign", System.Globalization.NumberFormatInfo.CurrentInfo.PositiveSign);
			array.Add("negative_sign", System.Globalization.NumberFormatInfo.CurrentInfo.NegativeSign);
			array.Add("int_frac_digits", System.Globalization.NumberFormatInfo.InvariantInfo.NumberDecimalDigits);
			array.Add("frac_digits", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalDigits);
			//array.Add("p_cs_precedes", );
			//array.Add("p_sep_by_space", );
			//array.Add("n_cs_precedes", );
			//array.Add("n_sep_by_space", );
			//array.Add("p_sign_posn", );
			//array.Add("n_sign_posn", );

			return array;
		}

		/// <summary>
		/// Calculates the md5 hash of a given file.
		/// </summary>
		/// <param name="filename">The name of the file which contents will be used to calculate the md5 hash.</param>
		/// <returns>Returns a string value representing the calculated md5 hash.</returns>
		public static string ComputeMD5FromFile(string filename)
		{
			System.Security.Cryptography.MD5CryptoServiceProvider crypto = new System.Security.Cryptography.MD5CryptoServiceProvider();
			System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
			System.IO.FileStream stream;
			string result;
			
			try
			{
				stream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				result = utf.GetString(crypto.ComputeHash(stream));
			}
			catch (System.Exception exception)
			{
				throw exception;
			}
		
			return result;
		}

		/// <summary>
		/// Calculates the md5 hash of a given string.
		/// </summary>
		/// <param name="str">The string that will be used to calculate the md5 hash.</param>
		/// <returns>Returns a string value representing the calculated md5 hash.</returns>
		public static string ComputeMD5(string str)
		{
			return System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(str,"md5").ToLower();
		}

		/// <summary>
		/// Inserts HTML line breaks before all newlines in a string.
		/// </summary>
		/// <param name="str">The string where the newline sequence will be replaced.</param>
		/// <returns>Returns string with newline sequences replaced with '<br />'.</returns>
		public static string Nl2br(string str)
		{
			string result = str;

			if (str.IndexOf("\r\n") > -1)
				result = str.Replace("\r\n", "<br />");
			else if (str.IndexOf("\n\r") > -1)
				result = str.Replace("\n\r", "<br />");
			else if (str.IndexOf("\r") > -1)
				result = str.Replace("\r", "<br />");
			else if (str.IndexOf("\n") > -1)
				result = str.Replace("\n", "<br />");

			return result;
		}

		/// <summary>
		/// Adds a backslash character (\) before every character that is among the following: . \\ + * ? [ ^ ] ( $ )
		/// </summary>
		/// <param name="str">The string where the characters will be escaped.</param>
		/// <returns>Returns a string with a backslash character before every special character.</returns>
		public static string QuoteMeta(string str)
		{
			string tempStr = "";
			tempStr = str.Replace(@"\", @"\\");
			tempStr = tempStr.Replace(".", "\\.");
			tempStr = tempStr.Replace("+", "\\+");
			tempStr = tempStr.Replace("*", "\\*");
			tempStr = tempStr.Replace("?", "\\?");
			tempStr = tempStr.Replace("[", "\\[");
			tempStr = tempStr.Replace("^", "\\^");
			tempStr = tempStr.Replace("]", "\\]");
			tempStr = tempStr.Replace("(", "\\(");
			tempStr = tempStr.Replace("$", "\\$");
			tempStr = tempStr.Replace(")", "\\)");
			return tempStr;
		}

		/// <summary>
		/// Calculates the md5 hash of a given file.
		/// </summary>
		/// <param name="filename">The name of the file which contents will be used to calculate the md5 hash.</param>
		/// <returns>Returns a string value representing the calculated md5 hash.</returns>
		public static string ComputeSHA1FromFile(string filename)
		{
			System.Security.Cryptography.SHA1Managed crypto = new System.Security.Cryptography.SHA1Managed();
			System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
			System.IO.FileStream stream;
			string result;

			try
			{
				stream = new System.IO.FileStream(filename, System.IO.FileMode.Open, System.IO.FileAccess.Read);
				result = utf.GetString(crypto.ComputeHash(stream));
			}
			catch (System.Exception exception)
			{
				throw exception;
			}
		
			return result;
		}

		/// <summary>
		/// Calculates the md5 hash of a given string.
		/// </summary>
		/// <param name="str">The string that will be used to calculate the md5 hash.</param>
		/// <returns>Returns a string value representing the calculated md5 hash.</returns>
		public static string ComputeSHA1(string str)
		{
			System.Security.Cryptography.SHA1Managed crypto = new System.Security.Cryptography.SHA1Managed();
			System.Text.UTF8Encoding utf = new System.Text.UTF8Encoding();
			string result;
			
			try
			{
				result = utf.GetString(crypto.ComputeHash(utf.GetBytes(str)));
			}
			catch (System.Exception exception)
			{
				throw exception;
			}

			return result;
		}

		/// <summary>
		/// Pads a string to a certain length with the first character of the provided string.
		/// </summary>
		/// <param name="input">The string to pad.</param>
		/// <param name="length">The padding length.</param>
		/// <param name="padString">The string whose first character will be used to pad 'input'.</param>
		/// <param name="type">The type of padding.
		/// <list type="bullet">
		///   <item><description>0: Pad right.</description></item>
		///   <item><description>1: Pad left.</description></item>
		///   <item><description>2: Pad both.</description></item>
		/// </list>
		/// </param>
		/// <returns>Returns a new string that is equivalent 'input' but padded on the left, right or both sides 
		/// using first character of 'padString' and with a total length specified by 'lenght'.</returns>
		public static string Pad(string input, int length, string padString, int type)
		{
			string result = "";

			switch (type)
			{
					//PAD_RIGHT
				case 0: result = input.PadRight(length, padString[0]);
					break;
					//PAD_LEFT
				case 1: result = input.PadLeft(length, padString[0]);
					break;
					//PAD_BOTH
				case 2: result = input.PadLeft(input.Length + (int)System.Math.Floor(((double)length - (double)input.Length) / 2), padString[0]);
					result = result.PadRight(length, padString[0]);
					break;
			}

			return result;
		}

		/// <summary>
		/// Makes a binary comparison between the two specified strings. The case sensitiveness is parameter dependant.
		/// </summary>
		/// <param name="input1">The first string to compare.</param>
		/// <param name="input2">The second string to compare.</param>
		/// <param name="caseSensitive">The boolean value that specifies the case sensitiveness, if true, the comparison will be case-sensitive.</param>
		/// <returns>Returns:
		/// <list type="bullet">
		/// <item><description>-1: If the first string is less than the second one.</description></item>
		/// <item><description>1: If the second string is less than the first one.</description></item>
		/// <item><description>0: If both strings are equals.</description></item>
		/// </list>
		/// </returns>
		public static int StringCompare(string input1, string input2, bool caseSensitive)
		{
			int result = 0;
			string theInput1 = input1;
			string theInput2 = input2;

			if (!caseSensitive)
			{
				theInput1 = theInput1.ToLower();
				theInput2 = theInput2.ToLower();
			}

			if (theInput1 != theInput2)
			{
				if (theInput1.Length < theInput2.Length)
					result = -1;
				else if (theInput1.Length > theInput2.Length)
					result = 1;
				else
				{
					long res1 = SumStringBytes(theInput1);
					long res2 = SumStringBytes(theInput2);
					if (res1 < res2)
						result = -1;
					else 
						result = 1;
				}
			}
			return result;
		}

		/// <summary>
		/// Makes a binary comparison between the two specified strings. The case sensitiveness is parameter dependant.
		/// </summary>
		/// <param name="input1">The first string to compare.</param>
		/// <param name="input2">The second string to compare.</param>
		/// <param name="caseSensitive">The boolean value that specifies the case sensitiveness, if true, the comparison will be case-sensitive.</param>
		/// <param name="length">The number of characters to be compared on each string.</param>
		/// <returns>Returns:
		/// <list type="bullet">
		/// <item><description>-1: If the first string is less than the second one.</description></item>
		/// <item><description>1: If the second string is less than the first one.</description></item>
		/// <item><description>0: If both strings are equals.</description></item>
		/// </list></returns>
		public static int StringCompare(string input1, string input2, bool caseSensitive, int length)
		{
			return StringCompare(input1.Length <= length ? input1 : input1.Substring(0, length),
				input2.Length <= length ? input2 : input2.Substring(0, length), caseSensitive);
		}

		/// <summary>
		/// Sums the byte value of each character of the specified string.
		/// </summary>
		/// <param name="input">The string which contains the characters to sum.</param>
		/// <returns>Returns the sum of the byte value of each character of the specified string.</returns>
		private static long SumStringBytes(string input)
		{
			long sum = 0;
			for(int index=0; index<input.Length; index++)
			{
				sum += (byte)input[index];
			}
			return sum;
		}

		/// <summary>
		/// Returns the substring that starts at the last index of <code>occurrence</code> of the specified string.
		/// </summary>
		/// <param name="input">The string to obtaing the substring from.</param>
		/// <param name="occurrence">The object used to obtain the last index.</param>
		/// <returns>Returns the resulting substring.</returns>
		public static string LastSubstring(string input, object occurrence)
		{
			string result = string.Empty, startChar = "";

			if (occurrence is string)
				startChar = ((string)occurrence).Substring(0, 1);
			else if (occurrence is int)
				startChar = new string(System.Convert.ToChar((int)occurrence), 1);
			else
				startChar = occurrence.ToString();

			int index = input.LastIndexOf(startChar);
			if (index != -1)
				result = input.Substring(index);

			return result;
		}

		/// <summary>
		/// Reverses the specified string.
		/// </summary>
		/// <param name="input">The string to be reversed.</param>
		/// <returns>Returns the reversed string.</returns>
		public static string StringReverse(string input)
		{
			char[] theString = input.ToCharArray();
			System.Array.Reverse(theString);
			return new string(theString);
		}

		/// <summary>
		/// Returns the substring that starts at the last index of <code>occurrence</code> of the specified string.
		/// </summary>
		/// <param name="input">The string to obtaing the substring from.</param>
		/// <param name="occurrence">The object used to obtain the last index.</param>
		/// <returns>Returns the resulting substring.</returns>
		public static int LastIndexOf(string input, object occurrence)
		{
			string startChar = "";

			if (occurrence is string)
				startChar = ((string)occurrence).Substring(0, 1);
			else if (occurrence is int)
				startChar = new string(System.Convert.ToChar((int)occurrence), 1);
			else
				startChar = occurrence.ToString();

			return input.LastIndexOf(startChar);
		}

		private static string tokenizedString;

		/// <summary>
		/// Tokenizes the specified string using the specified separators.
		/// </summary>
		/// <param name="input">The string to tokenized.</param>
		/// <param name="separators">The string that contains the separators.</param>
		/// <returns>Returns the current token.</returns>
		public static string StringTokenizer(string input, string separators)
		{
			string tokenList = input, currentToken = "";
			char[] separatorArray = separators.ToCharArray();
			string[] tokensArray;
			
			tokenizedString = input;

			while (currentToken == "")
			{	
				tokensArray = tokenizedString.Split(separatorArray, 2);
				if (tokensArray.Length == 2)
				{
					currentToken = tokensArray[0];
					tokenizedString = tokensArray[1];
				}
				else
				{
					currentToken = tokensArray[0];
					tokenizedString = "";
					break;
				}
			}

			return currentToken;
		}

		/// <summary>
		/// Tokenizes a previously set string (to set the string to be tokenized, use the other overloaded method) using the specified separators.
		/// </summary>
		/// <param name="separators">The string that contains the separators.</param>
		/// <returns>Returns the current token.</returns>
		public static string StringTokenizer(string separators)
		{
			return StringTokenizer(tokenizedString, separators);
		}


		/// <summary>
		/// Replaces each character of <code>toFind</code> with the corresponding ones in <code>replaceWith</code> of the specified string.
		/// </summary>
		/// <param name="input">The string to used to do the replacements.</param>
		/// <param name="toFind">The string that contains the characters to find.</param>
		/// <param name="replaceWith">The string that contains the characters to replace with.</param>
		/// <returns>Returns a new replaced string.</returns>
		public static string StringReplace(string input, string toFind, string replaceWith)
		{
			int maxToReplace = toFind.Length <= replaceWith.Length ? toFind.Length : replaceWith.Length;
			string target = input;
			for (int index=0; index<maxToReplace; index++)
			{
				target = target.Replace(toFind[index], replaceWith[index]);
			}
			return target;
		}

		/// <summary>
		/// Counts the number of ocurrences of the specified substring in the specified string.
		/// </summary>
		/// <param name="input">The string where the substring is searched.</param>
		/// <param name="toFind">The substring whose occurrences will be counted.</param>
		/// <returns>Returns the number of occurrences of the specified substring.</returns>
		public static int SubstringCount(string input, string subString)
		{
			string theInput = input;
			int result = 0, index = 0;

			while (index != -1)
			{
				index = theInput.IndexOf(subString);
				if (index != -1)
				{
					result++;
					theInput = theInput.Remove(index, subString.Length);
				}
			}

			return result;
		}

		/// <summary>
		/// Returns a string with the first letter in uppercase.
		/// </summary>
		/// <param name="input">The string which whose first character will be changed.</param>
		/// <returns>Returns a string with the first letter in uppercase.</returns>
		public static string StringToUpperFirst(string input)
		{
			string result = "";
			if (input.Length > 1)
			{
				string toUpper = input.Substring(0, 1);
				toUpper = toUpper.ToUpper();
				result = toUpper + input.Substring(1);
			}
			else if (input.Length == 1)
				result = input.ToUpper();

			return result;
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to variable functions.
	/// </summary>
	public class VariableSupport
	{
		/// <summary>
		/// Returns 'true' if the given parameter is empty/null or has a zero-value.
		/// Note that objects will return false at all times.
		/// </summary>
		/// <param name="obj">The object of any type.</param>
		/// <returns>Returns false if object has a non-empty or non-zero value, true otherwise.</returns>
		public static bool Empty(object obj)
		{
			if (obj != null)
			{
				if (obj is System.String)
					return ((string)obj == System.String.Empty || (string)obj == "0");
				else if (obj is System.Int32)
					return ((int)obj == 0);
				else if (obj is System.Double)
					return ((double)obj == 0);
				else if (obj is System.Boolean)
					return ((bool)obj == false);
				else if (obj.GetType() == typeof(OrderedMap))
					return (((OrderedMap)obj).Count == 0);
				else //objects with [empty] properties returns false at all times.
					return false;
			}
			else
				return true;
		}

		/// <summary>
		/// Returns the float representation of the specified object.
		/// If the float value could not be retrieved, then the method will return zero.
		/// </summary>
		/// <param name="obj">The object as string.</param>
		/// <returns>Returns the float matched value.</returns>
		public static double FloatVal(object obj)
		{
			double result = 0;
			if (obj != null)
			{
				System.Text.RegularExpressions.Match match = null;
				try
				{
					match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?");
				}
				catch (System.FormatException) {}
				result = (match.Success? System.Convert.ToDouble(match.Value) : 0);
			}
			return result;
		}

		/// <summary>
		/// Returns the integer representation of the specified object in the specified base. 
		/// If the integer value could not be retrieved, then the method will return zero.
		/// It will work on bases 2, 8, 10 and 16 only.
		/// </summary>
		/// <param name="obj">The object as string.</param>
		/// <param name="_base">The numerical base, can be 2, 8, 10 or 16.</param>
		/// <returns>The integer matched value.</returns>
		public static int IntVal(object obj, int _base)
		{
			int result = 0;
			if (obj != null)
			{
				System.Text.RegularExpressions.Match match = null;
				try
				{
					switch(_base)
					{
						case 2:
							match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-1]+");
							break;
						case 8:
							match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-7]+");
							break;
						case 10:
							match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[0-9]+");
							break;
						case 16:
							match = System.Text.RegularExpressions.Regex.Match(obj.ToString(), "^[-+]?[a-fA-F0-9]+");
							break;
					}
				}
				catch (System.FormatException) {}
				if (match.Success)
				{
					if (match.Value.IndexOf('-') != -1)
						result = (0 - System.Convert.ToInt32(match.Value.Substring(1), _base));
					else
						result = System.Convert.ToInt32(match.Value, _base);
				}
				else
					result = 0;
			}
			return result;
		}

		/// <summary>
		/// Makes no distinction between a number or a numerical string; returns true for any of these.
		/// </summary>
		/// <param name="obj">The object to test.</param>
		/// <returns>Returns true if object contains a numerical value, false otherwise.</returns>
		public static bool IsNumeric(object obj)
		{
			bool result = false;
			if (obj != null)
			{
				result = System.Text.RegularExpressions.Regex.IsMatch(obj.ToString(), "^[-+]?[0-9]+[.]?[0-9]*([eE][-+]?[0-9]+)?$");
			}
			return result;
		}

		/// <summary>
		/// Returns true if the object passed as parameter is a scalar type, false otherwise.
		/// </summary>
		/// <param name="obj">The object to test.</param>
		/// <returns>Returns true if the object is scalar, false otherwise.</returns>
		public static bool IsScalar(object obj)
		{
			return (obj is System.String ||
				obj is System.Int32 ||
				obj is System.Int64 ||
				obj is System.Double ||
				obj is System.Boolean);
		}

		/// <summary>
		/// Gets a boolean value that indicates whether the specified value is a method or not.
		/// </summary>
		/// <param name="method">The value that may contain a method name (string) or an OrderedMap.
		/// If the value is an OrderedMap, then it should have to entries: a class instance and a method name.</param>
		/// <param name="callableName">The referenced string value used to return the method name.</param>
		/// <param name="declaringType">The method declaring type. This parameter is used when the first parameter is a string.</param>
		/// <returns>Returns a boolean value that indicates whther the specified value is a method or not.</returns>
		public static bool IsCallable(object method, ref string callableName, System.Type declaringType)
		{
			bool result = false;
			try
			{
				System.Reflection.MethodInfo methodInfo = null;

				if (method is System.String)
				{
					callableName = method.ToString();
					methodInfo = declaringType.GetMethod(method.ToString());
					if (methodInfo != null)
					{
						callableName = methodInfo.Name;
						result = true;
					}
				}
				else if (method is OrderedMap)
				{
					OrderedMap typeMethod = (OrderedMap)method;
					callableName = typeMethod.ToString();
					methodInfo = typeMethod.GetValueAt(0).GetType().GetMethod(typeMethod.GetValueAt(1).ToString());
					if (methodInfo != null)
					{
						callableName =  typeMethod.GetValueAt(0).GetType().Name + ":" + methodInfo.Name;
						result = true;
					}
				}
				else
				{
					callableName = method.ToString();
				}
			}
			catch
			{
			}

			return result;
		}

		/// <summary>
		/// Gets a boolean value that indicates whether the specified value is a method or not.
		/// </summary>
		/// <param name="method">The value that may contain a method name (string) or an OrderedMap.
		/// If the value is an OrderedMap, then it should have to entries: a class instance and a method name.</param>
		/// <param name="declaringType">The method declaring type. This parameter is used when the first parameter is a string.</param>
		/// <returns>Returns a boolean value that indicates whther the specified value is a method or not.</returns>
		public static bool IsCallable(object method, System.Type declaringType)
		{
			string callableName = "";
			return IsCallable(method, ref callableName, declaringType);
		}

		/// <summary>
		/// Returns true if the specified object is not a scalar type nor an OrderedMap.
		/// </summary>
		/// <param name="var">The object to test.</param>
		/// <returns>Returns true if the specified object is not a scalar type nor an OrderedMap.</returns>
		public static bool IsObject(object var)
		{
			return !VariableSupport.IsScalar(var) && !(var is OrderedMap);
		}

		/// <summary>
		/// Prints human-readable representation of the specified object array.
		/// </summary>
		/// <param name="vars">The object array to be printed.</param>
		public static void PrintHumanReadable(params object[] vars)
		{
			for(int index=0; index<vars.Length; index++)
			{
				PrintHumanReadable(vars[index], false);
			}
		}

		/// <summary>
		/// Prints or returns a human-readable representation of the specified object.
		/// </summary>
		/// <param name="var">The object to be printed or returned.</param>
		/// <param name="returnValue">A boolean value that indicates whether the value string representation should be printed out or returned.</param>
		/// <returns>Returns a string value, if <code>returnValue</code> is true, then the human-readable representation is returned, otherwise returns 1.</returns>
		public static string PrintHumanReadable(object var, bool returnValue)
		{
			int indentLevel = 0;
			return _PrintHumanReadable(var, returnValue, ref indentLevel);
		}

		/// <summary>
		/// Prints or returns a human-readable representation of the specified object.
		/// </summary>
		/// <param name="var">The object to be printed or returned.</param>
		/// <param name="returnValue">A boolean value that indicates whether the value string representation should be printed out or returned</param>
		/// <param name="indentLevel">This argument is used for indentation.</param>
		/// <returns>Returns a string value, if <code>returnValue</code> is true, then the human-readable representation is returned, otherwise returns 1.</returns>
		private static string _PrintHumanReadable(object var, bool returnValue, ref int indentLevel)
		{
			string result = "";
			if (var != null)
			{
				if (!IsObject(var))
				{
					if (var is OrderedMap)
					{
						int orderedMapLevel = indentLevel == 0 ? indentLevel : indentLevel + 1;
						result = ((OrderedMap)var).ToStringContents(ref orderedMapLevel).TrimEnd() + "\r\n";
					}
					else
					{
						if (var is System.Boolean)
							result = (bool)var ? "1":"";
						else
							result = var.ToString();
					}
				}
				else
				{
					System.Type theType = var.GetType();

					indentLevel++;
					string indentSpaces = new string(' ', (indentLevel-1) * 4);
					string classFields = theType.Name.ToLower() + " Object\r\n" + indentSpaces + "(\r\n";

					System.Reflection.FieldInfo[] theFields = theType.GetFields();
					foreach (System.Reflection.FieldInfo field in theFields)
					{
						indentSpaces = new string(' ', indentLevel * 4);

						object fieldValue = field.GetValue(var);
						string fieldValueString = "";
						if (fieldValue != null)
						{
							if (IsObject(fieldValue))
							{
								indentLevel++;
								fieldValueString = _PrintHumanReadable(fieldValue, true, ref indentLevel);
								indentLevel--;
							}
							else
							{
								fieldValueString = _PrintHumanReadable(fieldValue, true, ref indentLevel);
							}
						}

						classFields += indentSpaces + "[" + field.Name + "] => " + fieldValueString + "\r\n";
					}

					indentSpaces = new string(' ', (indentLevel-1) * 4);
					indentLevel--;
					classFields += indentSpaces + ")\r\n";
					result = classFields;
				}
			}
			if (!returnValue)
			{
				System.Web.HttpContext.Current.Response.Write(result);
				result = "1";
			}

			return result;
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to Date/Time functions.
	/// </summary>
	public class DateTimeSupport 
	{
		/// <summary>
		/// Returns true if the specified values represent a valid date, false otherwise.
		/// </summary>
		/// <param name="month">The month of the date to be checked.</param>
		/// <param name="day">The day of the date to be checked.</param>
		/// <param name="year">The year of the date to be checked.</param>
		/// <returns>Returns true if the specified values represent a valid date, false otherwise.</returns>
		public static bool CheckDate(int month, int day, int year)
		{
			bool result;
			try
			{
				new System.DateTime(year, month, day);
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Creates a new System.DateTime instance using the specified values. 
		/// If any of the values is equals to -1, then the equivalent value of the current date will be used.
		/// </summary>
		/// <param name="hour">The hour of the new System.DateTime.</param>
		/// <param name="minute">The minute of the new System.DateTime.</param>
		/// <param name="second">The second of the new System.DateTime.</param>
		/// <param name="month">The month of the new System.DateTime.</param>
		/// <param name="day">The day of the new System.DateTime.</param>
		/// <param name="year">The year of the new System.DateTime.</param>
		/// <returns></returns>
		public static System.DateTime NewDateTime(int hour, int minute, int second, int month, int day, int year)
		{
			System.DateTime now = System.DateTime.Now;
			int theHour = hour == -1 ? now.Hour : hour;
			int theMinute = minute == -1 ? now.Minute : minute;
			int theSecond = second == -1 ? now.Second : second;
			int theMonth = month == -1 ? now.Month : month;
			int theDay = day == -1 ? now.Day : day;
			int tempYear = (0 <= year && year <= 69) ? year + 2000 : 0;
			tempYear = (70 <= year && year <= 99) ? year + 1900 : year;
			int theYear = year == -1 ? now.Year : tempYear;

			return new System.DateTime(theYear, theMonth, theDay, theHour, theMinute, theSecond);
		}

		/// <summary>
		/// Creates a new System.DateTime from the specified timestamp (time measured in the number of seconds).
		/// </summary>
		/// <param name="timestamp">The timestamp that represents the date to be created.</param>
		/// <returns>Returns a new System.DateTime instance created from the specified timestamp.</returns>
		public static System.DateTime NewDateTime(int timestamp)
		{
			long initialTicks = new System.DateTime(1970,1,1,0,0,0,0).Ticks;
			long elapsedUtcTicks = System.Convert.ToInt64(timestamp) * 10000000;
			elapsedUtcTicks += initialTicks;
			System.DateTime theDate = new System.DateTime(elapsedUtcTicks).ToLocalTime();
			return theDate;
		}

		/// <summary>
		/// Creates a new OrderedMap that contains the date information associated with the specified timestamp.
		/// </summary>
		/// <param name="timestamp">The timestamp that represents the date to extract the information from.</param>
		/// <returns>Returns a new OrderedMap that contains the specified date information.</returns>
		public static OrderedMap GetDate(int timestamp)
		{
			OrderedMap dateInfo = new OrderedMap();
			System.DateTime theDate = NewDateTime(timestamp);
			dateInfo["seconds"] = theDate.Second;
			dateInfo["minutes"] = theDate.Minute;
			dateInfo["hours"] = theDate.Hour;
			dateInfo["mday"] = theDate.Day;
			switch (theDate.DayOfWeek)
			{
				case System.DayOfWeek.Sunday: dateInfo["wday"] = 0; break;
				case System.DayOfWeek.Monday: dateInfo["wday"] = 1; break;
				case System.DayOfWeek.Tuesday: dateInfo["wday"] = 2; break;
				case System.DayOfWeek.Wednesday: dateInfo["wday"] = 3; break;
				case System.DayOfWeek.Thursday: dateInfo["wday"] = 4; break;
				case System.DayOfWeek.Friday: dateInfo["wday"] = 5; break;
				default: dateInfo["wday"] = 6;	break;	//System.DayOfWeek.Saturday
			}
			dateInfo["mon"] = theDate.Month;
			dateInfo["year"] = theDate.Year;
			dateInfo["yday"] = theDate.DayOfYear;
			dateInfo["weekday"] = theDate.DayOfWeek.ToString();
			switch (theDate.Month)
			{
				case 1: dateInfo["month"] = "January"; break;
				case 2: dateInfo["month"] = "February"; break;
				case 3: dateInfo["month"] = "March"; break;
				case 4: dateInfo["month"] = "April"; break;
				case 5: dateInfo["month"] = "May"; break;
				case 6: dateInfo["month"] = "June"; break;
				case 7: dateInfo["month"] = "July"; break;
				case 8: dateInfo["month"] = "August"; break;
				case 9: dateInfo["month"] = "September"; break;
				case 10: dateInfo["month"] = "Octuber"; break;
				case 11: dateInfo["month"] = "November"; break;
				default: dateInfo["month"] = "December"; break;	//12
			}
			dateInfo[0] = timestamp;
			
			return dateInfo;
		}

		/// <summary>
		/// Returns an OrderedMap that contains the same structure returned by the C function call.
		/// </summary>
		/// <param name="timestamp">The timestamp that represents the date to extract the information from.</param>
		/// <param name="associative">The value that indicates whether to return an associative OrderedMap or a numerical one.</param>
		/// <returns>Returns an OrderedMap that contains information about the specified date.</returns>
		public static OrderedMap LocalTime(int timestamp, bool associative)
		{
			OrderedMap dateInfo = new OrderedMap();
			System.DateTime theDate = NewDateTime(timestamp);
			dateInfo[associative ? "tm_sec" : "0"] = theDate.Second;
			dateInfo[associative ? "tm_min" : "1"] = theDate.Minute;
			dateInfo[associative ? "tm_hour" : "2"] = theDate.Hour;
			dateInfo[associative ? "tm_mday" : "3"] = theDate.Day;
			dateInfo[associative ? "tm_mon" : "4"] = theDate.Month - 1;	//Month of the year starting with 0 for January.
			dateInfo[associative ? "tm_year" : "5"] = theDate.Year - 1900;	//Years since 1900.
			switch (theDate.DayOfWeek)
			{
				case System.DayOfWeek.Sunday: dateInfo[associative ? "tm_wday" : "6"] = 0; break;
				case System.DayOfWeek.Monday: dateInfo[associative ? "tm_wday" : "6"] = 1; break;
				case System.DayOfWeek.Tuesday: dateInfo[associative ? "tm_wday" : "6"] = 2; break;
				case System.DayOfWeek.Wednesday: dateInfo[associative ? "tm_wday" : "6"] = 3; break;
				case System.DayOfWeek.Thursday: dateInfo[associative ? "tm_wday" : "6"] = 4; break;
				case System.DayOfWeek.Friday: dateInfo[associative ? "tm_wday" : "6"] = 5; break;
				default: dateInfo["wday"] = 6;	break;	//System.DayOfWeek.Saturday
			}
			dateInfo[associative ? "tm_yday" : "7"] = theDate.DayOfYear - 1;
			dateInfo[associative ? "tm_isdst" : "8"] = System.TimeZone.CurrentTimeZone.IsDaylightSavingTime(theDate) ? 1 : 0;

			return dateInfo;
		}

		/// <summary>
		/// Returns a string that contains the current time represented in milliseconds and seconds.
		/// </summary>
		/// <returns>Returns a string that contains the current time represented in milliseconds and seconds.</returns>
		public static string Microtime()
		{
			System.DateTime now = System.DateTime.Now;
			double millisecond = now.Millisecond / 1000.0;
			string result = millisecond.ToString() + " " + Timestamp(now);
			return result;
		}

		/// <summary>
		/// Returns an OrderedMap that contains information about the current date.
		/// </summary>
		/// <returns>Returns an OrderedMap that contains information about the current date.</returns>
		public static OrderedMap GetTimeOfDay()
		{
			OrderedMap dateInfo = new OrderedMap();
			System.DateTime now = System.DateTime.Now;
			dateInfo["sec"] = Timestamp(now);
			dateInfo["usec"] = now.Millisecond;
			dateInfo["minuteswest"] = System.TimeZone.CurrentTimeZone.GetUtcOffset(now).TotalMinutes * -1;
			dateInfo["dsttime"] = System.TimeZone.CurrentTimeZone.DaylightName;
			return dateInfo;
		}
		/// <summary>
		/// Returns the current time measured in the number of seconds.
		/// </summary>
		/// <returns>Returns the current time measured in the number of seconds since January 1 1970 00:00:00 GMT.</returns>
		public static int Time() 
		{
			long initialTicks = new System.DateTime(1970,1,1,0,0,0,0).Ticks;
			long todayTicks = System.DateTime.UtcNow.Ticks;
			int elapsedSeconds = System.Convert.ToInt32((todayTicks-initialTicks) / 10000000);
			return elapsedSeconds;
		}

		/// <summary>
		/// Returns the specified time measured in the number of seconds.
		/// </summary>
		/// <param name="dateTime">The System.DateTime to obtain the number of seconds from.</param>
		/// <returns>Returns the specified time measured in the number of seconds since January 1 1970 00:00:00 GMT.</returns>
		public static int Timestamp(System.DateTime dateTime)
		{
			long initialTicks = new System.DateTime(1970,1,1,0,0,0,0).Ticks;
			long dateTicks = dateTime.ToUniversalTime().Ticks;
			int elapsedSeconds = System.Convert.ToInt32((dateTicks-initialTicks) / 10000000);
			return elapsedSeconds;
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to file system functions.
	/// </summary>
	public class FileSystemSupport
	{
		/// <summary>
		/// Returns the base file name of the specified path.
		/// </summary>
		/// <param name="path">The full file path.</param>
		/// <param name="suffix">The suffix that will be removed from the result if the file ends with it.</param>
		/// <returns>Return the base file name of the specified path.</returns>
		public static string BaseName(string path, string suffix)
		{
			if (path.EndsWith(suffix))
				return System.IO.Path.GetFileNameWithoutExtension(path);
			else
				return System.IO.Path.GetFileName(path);
		}

		/// <summary>
		/// Gets the number of available bytes of the specified disk.
		/// </summary>
		/// <param name="disk">The disk to obtain the information for.<param>
		/// <returns>Returns number of available bytes of the specified disk.</returns>
		public static long GetDiskFreeSpace(string disk)
		{
			long result = 0;
			try
			{
				string root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(disk));
				root = root.Replace(@"\", ""); 
				System.Management.ManagementObject theDisk = new System.Management.ManagementObject("win32_logicaldisk.deviceid=\"" + root + "\"");
				theDisk.Get();
				result = System.Convert.ToInt64(theDisk["FreeSpace"]);
			}
			catch
			{}
			return result;
		}

		/// <summary>
		/// Gets total size of the specified disk.
		/// </summary>
		/// <param name="disk">The disk to obtain the information for.<param>
		/// <returns>Returns total size of the specified disk.</returns>
		public static long GetDiskSize(string disk)
		{
			long result = 0;
			try
			{
				string root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(disk));
				root = root.Replace(@"\", ""); 
				System.Management.ManagementObject theDisk = new System.Management.ManagementObject("win32_logicaldisk.deviceid=\"" + root + "\"");
				theDisk.Get();
				result = System.Convert.ToInt64(theDisk["Size"]);
			}
			catch
			{}
			return result;
		}

		/// <summary>
		/// Returns an OrderedMap with information about the specified path.
		/// </summary>
		/// <param name="path">The path to retrieve the information from.</param>
		/// <returns>Returns an OrderedMap with information about the specified path.</returns>
		public static OrderedMap PathInfo(string path)
		{
			OrderedMap pathInfo = null;
			try
			{
				pathInfo = new OrderedMap();
				pathInfo["dirname"] = System.IO.Path.GetDirectoryName(path);
				pathInfo["basename"] = System.IO.Path.GetFileName(path);
				pathInfo["extension"] = System.IO.Path.GetExtension(path);
			}
			catch
			{}
			return pathInfo;
		}

		/// <summary>
		/// Opens the specified file using the specified file mode and file access options.
		/// </summary>
		/// <param name="fileName">The name of the file to open.</param>
		/// <param name="options">The file mode and file access options.</param>
		/// <returns>Returns an opened System.IO.FileStream.</returns>
		/// <remarks>This function is not intented to work with URLs.</remarks>
		public static System.IO.FileStream FileOpen(string fileName, string options) 
		{
			System.IO.FileStream file = null;
			try
			{
				System.IO.FileMode fileMode = System.IO.FileMode.Open;
				System.IO.FileAccess fileAccess = System.IO.FileAccess.Read;

				if (options.EndsWith("b") || options.EndsWith("t"))
					options = options.Remove(options.Length-1, 1);

				switch (options)
				{
					case "r":
						fileMode = System.IO.FileMode.Open;
						fileAccess = System.IO.FileAccess.Read;
						break;
					case "r+":
						fileMode = System.IO.FileMode.Open;
						fileAccess = System.IO.FileAccess.ReadWrite;
						break;
					case "w":
						if (!System.IO.File.Exists(fileName))
						{
							file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
							file.Close();
						}
						fileMode = System.IO.FileMode.Truncate;
						fileAccess = System.IO.FileAccess.Write;
						break;
					case "w+":
						if (!System.IO.File.Exists(fileName))
						{
							file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
							file.Close();
						}
						fileMode = System.IO.FileMode.Truncate;
						fileAccess = System.IO.FileAccess.Write;
						break;
					case "a":
						if (!System.IO.File.Exists(fileName))
						{
							file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
							file.Close();
						}
						fileMode = System.IO.FileMode.Append;
						fileAccess = System.IO.FileAccess.Write;
						break;
					case "a+":
						if (!System.IO.File.Exists(fileName))
						{
							file = new System.IO.FileStream(fileName, System.IO.FileMode.CreateNew);
							file.Close();
						}
						fileMode = System.IO.FileMode.Append;
						fileAccess = System.IO.FileAccess.Write;
						break;
					default:
						fileMode = System.IO.FileMode.Open;
						fileAccess = System.IO.FileAccess.Read;
						break;
				}
				file = new System.IO.FileStream(fileName, fileMode,  fileAccess);
			}
			catch
			{
			}
			return file;
		}

		/// <summary>
		/// Reads a block of bytes from the file stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="length">The lenght of the data to be read.</param>
		/// <returns>Returns the string representation of the read block of bytes.</returns>
		public static string Read(System.IO.BinaryReader stream, long length)
		{
			int theLength = (int)length;
			string result = null;
			try
			{
				byte[] bytes = new byte[length];
				int readBytes = stream.Read(bytes, 0, theLength);
				if (readBytes > 0)
					result = System.Text.Encoding.ASCII.GetString(bytes, 0, readBytes);
			}
			catch
			{}
			return result;
		}

        /// <summary>
        /// Reads a block of bytes from the file stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="length">The lenght of the data to be read.</param>
        /// <returns>Returns the string representation of the read block of bytes.</returns>
        public static string Read(MiscUtil.IO.EndianBinaryReader stream, long length)
        {
            int theLength = (int)length;
            string result = null;
            try
            {
                byte[] bytes = new byte[length];
                int readBytes = stream.Read(bytes, 0, theLength);
                if (readBytes > 0)
                    result = System.Text.Encoding.ASCII.GetString(bytes, 0, readBytes);
            }
            catch
            { }
            return result;
        }

		/// <summary>
		/// Reads the contents of the specified file.
		/// </summary>
		/// <param name="fileName">The file name of the file to be read.</param>
		/// <returns>Returns the contents of the specified file.</returns>
		public static string ReadContents(string fileName)
		{
			string result = null;
			try
			{
				System.IO.StreamReader reader = new System.IO.StreamReader(fileName);
				string contents = reader.ReadToEnd();
				reader.Close();
				result = contents;
			}
			catch
			{}
			return result;
		}

        public static byte[] ReadContentBytes(string fileName)
        {
           
            System.IO.FileStream file = new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);    
            System.IO.BinaryReader reader = new System.IO.BinaryReader(file);
            int size = Convert.ToInt32(file.Length);
            var result = reader.ReadBytes(size);
            reader.Close();
            return result;
        }

		/// <summary>
		/// Reads a byte from the file stream and advances the read position one byte.
		/// </summary>
		/// <param name="stream">The file stream to read from.</param>
		/// <returns>Returns the string representation of the read byte.</returns>
		public static string ReadByte(System.IO.FileStream stream)
		{
			string result = null;
			try
			{
				result = System.Text.Encoding.ASCII.GetString(new byte[]{(byte)stream.ReadByte()});
			}
			catch
			{}
			return result;
		}

		/// <summary>
		/// Reads an entire file into an OrderedMap.
		/// </summary>
		/// <param name="fileName">The name of the file to open and read.</param>
		/// <returns>Returns an OrderedMap containing the data from the file.</returns>
		public static OrderedMap FileToArray(string fileName) 
		{
			OrderedMap result = null;
			try 
			{
				result = new OrderedMap();
				System.IO.StreamReader reader = new System.IO.StreamReader(fileName);
				string line = reader.ReadLine();
				while (line != null) 
				{
					result[line] = line;
					line = reader.ReadLine();
				}
				reader.Close();
			} 
			catch
			{
			}
			return result;
		}

		/// <summary>
		/// Closes the specified FileStream object.
		/// </summary>
		/// <param name="stream">The FileStream object to close.</param>
		/// <returns>Returns a boolean value that indicates whether the stream was successfully closed (true) or not (false).</returns>
		public static bool Close(System.IO.FileStream stream) 
		{
			bool result;
			try 
			{
				if (stream != null) stream.Close();
				result = true;
			}
			catch
			{
				result = false;
			}
			return result;
		}

		/// <summary>
		/// Writes the specified data in the specified FileStream object.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="data">The data to be written to the stream.</param>
		/// <param name="length">The lenght of the data to be written.</param>
		/// <returns>Returns the number of bytes written.</returns>
		public static int Write(System.IO.FileStream stream, string data, int length)
		{
			int resultLength = -1;
			try
			{
				if (length > 0 && length < data.Length) data = data.Substring(0, length);
				System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
				byte[] bytes = encoding.GetBytes(data);
				stream.Write(bytes, 0, bytes.Length);
				resultLength = (int)bytes.Length;
			}
			catch
			{}
			return resultLength;
		}

		/// <summary>
		/// Rewinds the positions of this stream to the beginning.
		/// </summary>
		/// <param name="stream">The stream to be rewinded.</param>
		/// <returns>Returns a boolean value that indicates whether the stream was successfully rewindde (true) or not (false).</returns>
		public static bool Rewind(System.IO.FileStream stream)
		{
			bool result = false;
			try
			{
				if (stream.CanSeek) 
				{
					stream.Seek(0, System.IO.SeekOrigin.Begin);
					result = true;
				}
			}
			catch
			{}
			return result;
		}

		/// <summary>
		/// Returns a booolean value that indicates whether the specified file or directory exists and is writable.
		/// </summary>
		/// <param name="fileName">The file or directory name to be checked.</param>
		/// <returns>Returns a booolean value that indicates whether the specified file or directory exists and is writable.</returns>
		public static bool IsWritable(string fileName)
		{
			bool result = false;
			try
			{
				if (System.IO.File.Exists(fileName) || System.IO.Directory.Exists(fileName))
				{
					System.IO.FileAttributes attributes = System.IO.File.GetAttributes(fileName);
					result = !((attributes & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly);
				}
			}
			catch
			{}
			return result;
		}

		/// <summary>
		/// Reads and outputs the contents of the specified file.
		/// </summary>
		/// <param name="fileName">The file name of the file to be read.</param>
		/// <returns>Returns the length of the data read.</returns>
		public static int OutputFile(string fileName)
		{
			int length = -1;
			try
			{
				string contents = ReadContents(fileName);
				System.Web.HttpContext.Current.Response.Write(contents);
				length = (int)contents.Length;
			}
			catch
			{}
			return length;
		}

		/// <summary>
		/// Returns an OrderedMap with the pathnames that match the specified pattern.
		/// </summary>
		/// <param name="pattern">The search pattern.</param>
		/// <returns>Returns an OrderedMap with the pathnames that match the specified pattern.</returns>
		public static OrderedMap Glob(string pattern)
		{
			OrderedMap newOrderedMap = null;
			try
			{
				string path = System.Web.HttpContext.Current.Request.MapPath(System.Web.HttpContext.Current.Request.ApplicationPath);
				System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(path));
				System.IO.FileSystemInfo[] fileInfos =  dirInfo.GetFiles(pattern);
				if (fileInfos.Length > 0)
				{
					newOrderedMap = new OrderedMap();
					for (int index=0; index<fileInfos.Length; index++)
						newOrderedMap[index] = fileInfos[index].Name;
				}
			}
			catch
			{}
			return newOrderedMap;
		}

		/// <summary>
		/// Reads the specified INI file and returns the contents in an OrderedMap.
		/// </summary>
		/// <param name="fileName">The INI file to read.</param>
		/// <returns>Returns the contents of the specified INI file.</returns>
		public static OrderedMap ParseINI(string fileName)
		{
			OrderedMap newOrderedMap = null;
			try
			{
				using (System.IO.StreamReader stream = new System.IO.StreamReader(fileName)) 
				{
					newOrderedMap = new OrderedMap();
					string line;
					while ((line = stream.ReadLine()) != null) 
					{
						line = line.Trim();
						if (line != "" && !line.StartsWith(";") && !line.StartsWith("["))
						{
							string[] lineContents = line.Split('=');
							newOrderedMap[lineContents[0].Trim()] = lineContents[1].Trim();
						}
					}
				}
			}
			catch
			{}
			return newOrderedMap;
		}

		/// <summary>
		/// Reads a line from the specified stream. 
		/// Reading ends when one of the following conditions is met:
		/// <list type="bullet">
		/// <item>Length - 1 bytes have been read.</item>
		/// <item>On a newline.</item>
		/// <item>On EOF.</item>
		/// </list>
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="length">The maximum length of the line to be read.</param>
		/// <returns>Returns a string value that represents a line.</returns>
		public static string ReadLine(System.IO.FileStream stream, int length)
		{
			string line = null;
			try
			{
				int count = 0;
				bool endOfLine = false;
				while(!endOfLine)
				{
					if (stream.Position < stream.Length && count < length)
					{
						//A line is defined as a sequence of characters followed by:
						//	a carriage return (hexadecimal 0x000d)
						//	a line feed (hexadecimal 0x000a)
						//	or carriage return + line feed (hexadecimal 0x000d 0x000a)
						byte theByte = (byte)stream.ReadByte();
						if (theByte == (byte)0x0d || theByte == (byte)0x0a)
						{
							byte nextByte = (byte)stream.ReadByte();
							if (nextByte != (byte)0x0a) stream.Position--;	//if line ends with 0x000d 0x000a, then consume 0x000a.
							endOfLine = true;
						}
						else
							line += System.Text.Encoding.ASCII.GetString(new byte[]{theByte});
					}
					else
						endOfLine = true;
				}
			}
			catch
			{}
			return line;
		}

		/// <summary>
		/// Reads a line from the specified stream and parses it for CSV fields.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="length">The maximum length of the line to be read</param>
		/// <param name="delimiter">The delimiter which separates the CSV fields.</param>
		/// <returns>Returns an OrderedMap that contains the CSV fields of the read line.</returns>
		public static OrderedMap ReadCSV(System.IO.FileStream stream, int length, string delimiter)
		{
			OrderedMap newOrderedMap = null;
			try
			{
				string line = ReadLine(stream, length);
				if (line != null)
				{
					if (delimiter == null || delimiter == string.Empty) delimiter = ",";
					string[] fields = line.Split(delimiter[0]);
					for(int index=0; index<fields.Length; index++)
						fields[index] = fields[index].Trim();

					newOrderedMap = new OrderedMap(fields, false);
				}
			}
			catch
			{}
			return newOrderedMap;
		}

		/// <summary>
		/// Returns a string value that indicates whether the specified path is a file (file) or a directory ("dir").
		/// </summary>
		/// <param name="fileName">The path to be checked.</param>
		/// <returns>Returns a string value that indicates whether the specified path is a file ("file") or a directory ("dir").</returns>
		public static string FileType(string path)
		{
			string result = "";
			if (System.IO.File.Exists(path))
				result = "file";
			else if (System.IO.Directory.Exists(path))
				result = "dir";
			else
				result = "unkown";
			
			return result;
		}

		/// <summary>
		/// Outputs all remaining data in a file stream to the current HTTP output content stream. It also closes the stream.
		/// </summary>
		/// <param name="stream">The stream to read the data from.</param>
		/// <returns>Returns the number of bytes written.</returns>
		public static int OutputContents(System.IO.BinaryReader stream)
		{
			int length = -1;
			try
			{
				byte[] result =  stream.ReadBytes(Convert.ToInt32(stream.BaseStream.Length - stream.BaseStream.Position));
				stream.Close();
				System.Web.HttpContext.Current.Response.Write(result);
				length = (int)result.Length;
			}
			catch
			{}
			return length;
		}

		/// <summary>
		/// Creates a temporal file and opens it.
		/// </summary>
		/// <returns>Returns a System.IO.FileStream object that represents the temporal file.</returns>
		public static System.IO.FileStream TempFile()
		{
			string fileName = System.IO.Path.GetTempFileName();
			System.IO.FileStream stream = FileOpen(fileName, "w+");
			return stream;
		}
	}
	/*******************************/
	/// <summary>
	/// Provides static methods related to Perl Regular Expressions functions.
	/// </summary>
	public class RegExPerlSupport
	{
		/// <summary>
		/// Performs a regular expression match. The OrderedMap parameter is populated with the matched 
		/// substrings. If flags is provided and is equal to 256 the index of the matched substring will
		/// also be included in the OrderedMap parameter.
		/// </summary>
		/// <param name="pattern">The pattern to search in subject.</param>
		/// <param name="subject">The subject string where the pattern will be applied.</param>
		/// <param name="matches">An OrderedMap object where the matched string will be stored, and its index
		/// in subject if the flags param equals 256.</param>
		/// <param name="flags">If set with 256, the index of the matched substring will be added to the
		/// OrderedMao parameter.</param>
		/// <returns>Returns 0 if no matches were found and 1 if a match was found.</returns>
		public static int Match(string pattern, string subject, ref OrderedMap matches, int flags)
		{
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);
			System.Text.RegularExpressions.Match match = regex.Match(subject);

			if (match.Success == false)
			{
				matches = new OrderedMap();
				return 0;
			}
			else
			{
				if (flags == 256)
				{
					matches = new OrderedMap(new OrderedMap(new object[] {match.Groups[0].Value, match.Groups[0].Index}));
				}
				else
				{
					matches = new OrderedMap(match.Groups[0].Value);
				}

				return 1;
			}
		}

		/// <summary>
		/// Perform a global regular expression match on the specified subject string.
		/// </summary>
		/// <param name="pattern">The pattern to search in subject.</param>
		/// <param name="subject">The subject string where the pattern will be applied.</param>
		/// <param name="matches">An OrderedMap object where the matched strings will be stored.</param>
		/// <returns>Returns the number of matches found in the subject string.</returns>
		public static int MatchAll(string pattern, string subject, ref OrderedMap matches)
		{
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
			System.Text.RegularExpressions.Match match;
			matches = new OrderedMap();
			int count = 0;
			for (match = regex.Match(subject); match.Success; match = match.NextMatch())
			{
				OrderedMap innerMatches = new OrderedMap();
				for (int i=0; i < match.Groups.Count; i++)
				{
					innerMatches.Add(i, match.Groups[i].Value);
				}
				matches.Add(count, innerMatches);
				count++;
			}
			
			return match.Groups.Count;
		}
		
		/// <summary>
		/// Splits the subject string at the position defined by the given regular expression.
		/// The limit parameter specifies the maximum number of times the string is to be split.
		/// </summary>
		/// <param name="pattern">The pattern to search in subject.</param>
		/// <param name="subject">The subject string where the pattern will be applied.</param>
		/// <param name="limit">The maximum number of array elements to return.</param>
		/// <returns>Returns an OrderedMap object containing substrings of subject split along 
		/// boundaries matched by pattern. </returns>
		public static OrderedMap Split(string pattern, string subject, int limit)
		{
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);
			string[] result;

			if (limit < 0)
				result = regex.Split(subject);
			else
				result = regex.Split(subject, limit);

			OrderedMap array = new OrderedMap();

			for (int i=0; i < result.Length; i++)
				array.Add(i, result[i]);

			return array;
		}

		/// <summary>
		/// Return the elements of the input OrderedMap that match the given pattern.
		/// </summary>
		/// <param name="pattern">The pattern to search in each OrderedMap entry.</param>
		/// <param name="input">The OrderedMap object whose elements will be searched for pattern.</param>
		/// <returns>An OrderedMap populated with the elements of input that match pattern.</returns>
		public static OrderedMap MatchArray(string pattern, OrderedMap input)
		{
			OrderedMap result = new OrderedMap();
			System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);

			foreach(object obj in input.Values)
			{
				if (regex.Match(obj.ToString()).Success == true)
					result.Push(new object[] {obj});
			}

			return result;
		}
	}


}
