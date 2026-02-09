using System;
using System.Collections;
using System.Collections.Generic;

namespace RCommon.Collections
{
	/// <summary>
	/// A dictionary that maps each key to a list of values, allowing multiple values per key.
	/// Automatically creates an empty list when accessing a key that does not yet exist.
	/// </summary>
	/// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
	/// <typeparam name="TValue">The type of the values stored in the lists.</typeparam>
	public class ListDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, List<TValue>>> where TKey : notnull
	{
		readonly Dictionary<TKey, List<TValue>> innerValues = new Dictionary<TKey, List<TValue>>();

		/// <summary>
		/// Gets the number of keys in the dictionary.
		/// </summary>
		public int Count
		{
			get { return innerValues.Count; }
		}

		/// <summary>
		/// Gets or sets the list of values associated with the specified key.
		/// If the key does not exist on get, an empty list is created and associated with the key.
		/// </summary>
		/// <param name="key">The key to look up.</param>
		/// <returns>The list of values for the specified key.</returns>
		public List<TValue> this[TKey key]
		{
			get
			{
				if (innerValues.ContainsKey(key) == false)
					innerValues.Add(key, new List<TValue>());

				return innerValues[key];
			}
			set { innerValues[key] = value; }
		}

		/// <summary>
		/// Gets a collection of all keys in the dictionary.
		/// </summary>
		public ICollection<TKey> Keys
		{
			get { return innerValues.Keys; }
		}

		/// <summary>
		/// Gets a flattened list of all values across all keys.
		/// </summary>
		public List<TValue> Values
		{
			get
			{
				List<TValue> values = new List<TValue>();

				foreach (List<TValue> list in innerValues.Values)
					values.AddRange(list);

				return values;
			}
		}

		/// <summary>
		/// Adds a key to the dictionary with an empty value list.
		/// </summary>
		/// <param name="key">The key to add. Must not be null.</param>
		public void Add(TKey key)
		{
			Guard.IsNotNull(key!, "key");

			CreateNewList(key);
		}

		/// <summary>
		/// Adds a value to the list associated with the specified key.
		/// If the key does not exist, a new list is created first.
		/// </summary>
		/// <param name="key">The key to add the value under. Must not be null.</param>
		/// <param name="value">The value to add. Must not be null.</param>
		public void Add(TKey key,
						TValue value)
		{
			Guard.IsNotNull(key!, "key");
			Guard.IsNotNull(value!, "value");

			if (innerValues.ContainsKey(key))
				innerValues[key].Add(value);
			else
			{
				List<TValue> values = CreateNewList(key);
				values.Add(value);
			}
		}

		/// <summary>
		/// Removes all keys and their associated value lists from the dictionary.
		/// </summary>
		public void Clear()
		{
			innerValues.Clear();
		}

		/// <summary>
		/// Determines whether the dictionary contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate. Must not be null.</param>
		/// <returns><c>true</c> if the dictionary contains the key; otherwise, <c>false</c>.</returns>
		public bool ContainsKey(TKey key)
		{
			Guard.IsNotNull(key!, "key");

			return innerValues.ContainsKey(key);
		}

		/// <summary>
		/// Determines whether any value list in the dictionary contains the specified value.
		/// </summary>
		/// <param name="value">The value to search for.</param>
		/// <returns><c>true</c> if the value is found in any list; otherwise, <c>false</c>.</returns>
		public bool ContainsValue(TValue value)
		{
			foreach (KeyValuePair<TKey, List<TValue>> pair in innerValues)
				if (pair.Value.Contains(value))
					return true;

			return false;
		}

		/// <summary>
		/// Creates a new empty value list and associates it with the specified key.
		/// </summary>
		/// <param name="key">The key to associate the new list with.</param>
		/// <returns>The newly created empty list.</returns>
		List<TValue> CreateNewList(TKey key)
		{
			List<TValue> values = new List<TValue>();
			innerValues.Add(key, values);
			return values;
		}

		/// <summary>
		/// Finds all values whose keys match the specified filter predicate.
		/// </summary>
		/// <param name="keyFilter">A predicate to filter keys.</param>
		/// <returns>An enumerable of values from all matching keys.</returns>
		public IEnumerable<TValue> FindByKey(Predicate<TKey> keyFilter)
		{
			foreach (KeyValuePair<TKey, List<TValue>> pair in this)
				if (keyFilter(pair.Key))
					foreach (TValue value in pair.Value)
						yield return value;
		}

		/// <summary>
		/// Finds all values where both the key and value match the specified filter predicates.
		/// </summary>
		/// <param name="keyFilter">A predicate to filter keys.</param>
		/// <param name="valueFilter">A predicate to filter values within matching keys.</param>
		/// <returns>An enumerable of values matching both filters.</returns>
		public IEnumerable<TValue> FindByKeyAndValue(Predicate<TKey> keyFilter,
													 Predicate<TValue> valueFilter)
		{
			foreach (KeyValuePair<TKey, List<TValue>> pair in this)
				if (keyFilter(pair.Key))
					foreach (TValue value in pair.Value)
						if (valueFilter(value))
							yield return value;
		}

		/// <summary>
		/// Finds all values across all keys that match the specified value filter predicate.
		/// </summary>
		/// <param name="valueFilter">A predicate to filter values.</param>
		/// <returns>An enumerable of matching values.</returns>
		public IEnumerable<TValue> FindByValue(Predicate<TValue> valueFilter)
		{
			foreach (KeyValuePair<TKey, List<TValue>> pair in this)
				foreach (TValue value in pair.Value)
					if (valueFilter(value))
						yield return value;
		}

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
		{
			return innerValues.GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return innerValues.GetEnumerator();
		}

		/// <summary>
		/// Removes the key and its entire associated value list from the dictionary.
		/// </summary>
		/// <param name="key">The key to remove. Must not be null.</param>
		/// <returns><c>true</c> if the key was found and removed; otherwise, <c>false</c>.</returns>
		public bool Remove(TKey key)
		{
			Guard.IsNotNull(key!, "key");

			return innerValues.Remove(key);
		}

		/// <summary>
		/// Removes all occurrences of a specific value from the list associated with the specified key.
		/// </summary>
		/// <param name="key">The key whose value list to modify. Must not be null.</param>
		/// <param name="value">The value to remove. Must not be null.</param>
		public void Remove(TKey key,
						   TValue value)
		{
			Guard.IsNotNull(key!, "key");
			Guard.IsNotNull(value!, "value");

			if (innerValues.ContainsKey(key))
				innerValues[key].RemoveAll(delegate (TValue item)
				{
					return value!.Equals(item);
				});
		}

		/// <summary>
		/// Removes a specific value from all keys' value lists in the dictionary.
		/// </summary>
		/// <param name="value">The value to remove from all lists.</param>
		public void Remove(TValue value)
		{
			foreach (KeyValuePair<TKey, List<TValue>> pair in innerValues)
				Remove(pair.Key, value);
		}
	}
}