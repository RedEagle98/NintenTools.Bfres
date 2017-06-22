﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Syroot.NintenTools.Bfres.Core;

namespace Syroot.NintenTools.Bfres
{
    /// <summary>
    /// Represents the non-generic base of a dictionary which can quickly look up <see cref="IResData"/> instances of
    /// type <typeparamref name="T"/> via key or index.
    /// </summary>
    /// <typeparam name="T">The specialized type of the <see cref="IResData"/> instances.</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(TypeProxy))]
    public abstract class ResDict : IEnumerable<KeyValuePair<string, IResData>>, IResData
    {
        // ---- FIELDS -------------------------------------------------------------------------------------------------

        protected IList<Node> _nodes; // Includes root node.

        // ---- CONSTRUCTORS & DESTRUCTOR ------------------------------------------------------------------------------

        protected ResDict()
        {
            // Create root node.
            _nodes = new List<Node> { new Node() };
        }

        // ---- PROPERTIES ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the number of instances stored.
        /// </summary>
        public int Count
        {
            get { return _nodes.Count - 1; }
        }

        /// <summary>
        /// Gets all keys under which instances are stored.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                for (int i = 1; i < _nodes.Count; i++)
                {
                    yield return _nodes[i].Key;
                }
            }
        }

        /// <summary>
        /// Gets all stored instances.
        /// </summary>
        internal IEnumerable<IResData> Values
        {
            get
            {
                for (int i = 1; i < _nodes.Count; i++)
                {
                    yield return _nodes[i].Value;
                }
            }
        }

        /// <summary>
        /// Returns only the publically visible nodes, excluding the root node.
        /// </summary>
        protected IEnumerable<Node> Nodes
        {
            get
            {
                for (int i = 1; i < _nodes.Count; i++)
                {
                    yield return _nodes[i];
                }
            }
        }

        // ---- OPERATORS ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the <see cref="IResData"/> instance stored at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The 0-based index of the <see cref="IResData"/> instance to get or set.</param>
        /// <returns>The <see cref="IResData"/> at the specified <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is smaller than 0 or bigger or equal to
        /// <see cref="Count"/>.</exception>
        internal IResData this[int index]
        {
            get
            {
                // Throw if index out of bounds.
                Lookup(index, out Node node);
                return node.Value;
            }
            set
            {
                // Throw if index out of bounds.
                Lookup(index, out Node node);
                node.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IResData"/> instance stored under the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The textual key of the <see cref="IResData"/> instance to get or set.</param>
        /// <returns>The <see cref="IResData"/> with the specified <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentException">An <see cref="IResData"/> instance with the same <paramref name="key"/>
        /// already exists.</exception>
        /// <exception cref="KeyNotFoundException">An <see cref="IResData"/> instance with the given
        /// <paramref name="key"/> does not exist.</exception>
        internal IResData this[string key]
        {
            get
            {
                // Throw if key does not exist.
                Lookup(key, out Node node, out int index);
                return node.Value;
            }
            set
            {
                // Throw if key does not exist.
                Lookup(key, out Node node, out int index);
                node.Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the key under which the specified <paramref name="instance"/> is stored.
        /// </summary>
        /// <param name="instance">The <see cref="IResData"/> instance of the key to get or set.</param>
        /// <returns>The key of the specified <paramref name="instance"/>.</returns>
        /// <exception cref="ArgumentException">An <see cref="IResData"/> instance with the same <paramref name="key"/>
        /// already exists.</exception>
        /// <exception cref="KeyNotFoundException">A key for the given <paramref name="instance"/> does not exist.
        /// </exception>
        internal string this[IResData instance]
        {
            get
            {
                // Throw if instance does not exist.
                Lookup(instance, out Node node, out int index);
                return node.Key;
            }
            set
            {
                // Throw if instance does not exist.
                Lookup(instance, out Node node, out int index);
                // Throw if keys would be duplicated.
                if (Lookup(value, out Node keyNode, out index, false))
                {
                    throw new ArgumentException($"Key \"{value}\" already exists.");
                }
            }
        }

        // ---- METHODS (PUBLIC) ---------------------------------------------------------------------------------------

        /// <summary>
        /// Removes all elements from the dictionary.
        /// </summary>
        public void Clear()
        {
            // Create new collection with root node.
            _nodes.Clear();
            _nodes.Add(new Node());
        }

        /// <summary>
        /// Determines whether an instance is saved under the given <paramref name="key"/> in the dictionary.
        /// </summary>
        /// <param name="key">The textual key to locate in the dictionary. The value can be <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="key"/> was found in the dictionary; otherwise <c>false</c>.</returns>
        public bool ContainsKey(string key)
        {
            return Lookup(key, out Node node, out int index, false);
        }

        /// <summary>
        /// Searches for the specified <paramref name="key"/> and returns the zero-based index of the first occurrence
        /// within the entire dictionary.
        /// </summary>
        /// <param name="key">The textual key to locate in the dictionary. The value can be <c>null</c>.</param>
        /// <returns>The zero-based index of the first occurence of <paramref name="key"/> within the entire dictionary
        /// if found; otherwise <c>-1</c>.</returns>
        public int IndexOf(string key)
        {
            return Lookup(key, out Node node, out int index, false) ? index : -1;
        }

        /// <summary>
        /// Removes the first occurrence of the instance with the specific <paramref name="key"/> from the dictionary.
        /// </summary>
        /// <param name="key">The textual key of the <see cref="IResData"/> instance which will be removed.</param>
        /// <returns><c>true</c> if the instance under <paramref name="key"/> was successfully removed; otherwise
        /// <c>false</c>. This method also returns <c>false</c> if <paramref name="key"/> was not found in the
        /// dictionary.</returns>
        public bool Remove(string key)
        {
            if (Lookup(key, out Node node, out int index, false))
            {
                _nodes.Remove(node);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the instance at the specified <paramref name="index"/> of the dictionary.
        /// </summary>
        /// <param name="index">The zero-based index of the instance to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or equal to or greater
        /// than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            // Throw if index out of bounds.
            Lookup(index, out Node node, true);
            _nodes.Remove(node);
        }

        /// <summary>
        /// Returns <c>true</c> if an <see cref="IResData"/> instance was stored under the given <paramref name="key"/>
        /// and has been assigned to <paramref name="value"/>, or <c>false</c> if no instance is stored under the
        /// given <paramref name="key"/> and <c>null</c> was assigned to <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The textual key of the <see cref="IResData"/> instance to get or set.</param>
        /// <param name="value">The variable receiving the found <see cref="IResData"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if an instance was found and assigned; otherwise <c>false</c>.</returns>
        internal bool TryGetValue(string key, out IResData value)
        {
            if (Lookup(key, out Node node, out int index, false))
            {
                value = node.Value;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        // ---- METHODS (INTERNAL) -------------------------------------------------------------------------------------

        /// <summary>
        /// Adds the given <paramref name="value"/> under the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The textual key under which the <see cref="IResData"/> instance will be stored.</param>
        /// <param name="value">The <see cref="IResData"/> to add.</param>
        /// <exception cref="ArgumentException">An <see cref="IResData"/> instance with the same <paramref name="key"/>
        /// already exists.</exception>
        internal void Add(string key, IResData value)
        {
            // Throw if key already exists.
            Lookup(key, out Node node, out int index);
            _nodes.Add(new Node(key, value));
        }

        /// <summary>
        /// Determines whether the given <paramref name="value"/> is in the dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to locate in the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="value"/> was found in the dictionary; otherwise <c>false</c>.
        /// </returns>
        internal bool ContainsValue(IResData value)
        {
            return Lookup(value, out Node node, out int index, false);
        }

        /// <summary>
        /// Searches for the specified <paramref name="value"/> and returns the zero-based index of the first occurrence
        /// within the entire dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to locate in the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns>The zero-based index of the first occurence of <paramref name="value"/> within the entire
        /// dictionary if found; otherwise <c>-1</c>.</returns>
        internal int IndexOf(IResData value)
        {
            return Lookup(value, out Node node, out int index, false) ? index : -1;
        }

        /// <summary>
        /// Removes the first occurrence of a specific <paramref name="value"/> from the dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to remove from the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="value"/> was successfully removed; otherwise <c>false</c>. This
        /// method also returns <c>false</c> if <paramref name="value"/> was not found in the dictionary.</returns>
        internal bool Remove(IResData value)
        {
            if (Lookup(value, out Node node, out int index, false))
            {
                _nodes.Remove(node);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Copies the elements of the dictionary as <see cref="KeyValuePair{String, IResData}"/> instances to a new
        /// array and returns it.
        /// </summary>
        /// <returns>An array containing copies of the elements.</returns>
        internal KeyValuePair<string, IResData>[] ToArray()
        {
            KeyValuePair<string, IResData>[] resData = new KeyValuePair<string, IResData>[Count];
            int i = 0;
            foreach (Node node in Nodes)
            {
                resData[i] = new KeyValuePair<string, IResData>(node.Key, node.Value);
                i++;
            }
            return resData;
        }

        /// <summary>
        /// Returns <c>true</c> if a key was found for the given <paramref name="value"/> and has been assigned to
        /// <paramref name="key"/>, or <c>false</c> if no key was found for the value and <c>null</c> was assigned to
        /// <paramref name="key"/>.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> to look up a key for.</param>
        /// <param name="key">The variable receiving the found key or <c>null</c>.</param>
        /// <returns><c>true</c> if a key was found and assigned; otherwise <c>false</c>.</returns>
        internal bool TryGetKey(IResData value, out string key)
        {
            if (Lookup(value, out Node node, out int index, false))
            {
                key = node.Key;
                return true;
            }
            else
            {
                key = null;
                return false;
            }
        }

        // ---- METHODS ------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns an <see cref="IEnumerator{KeyValuePair{String, IResData}}"/> which can be used to iterate over the
        /// items in the dictionary.
        /// </summary>
        /// <returns>An enumerator to iterate over the items in the dictionary.</returns>
        IEnumerator<KeyValuePair<string, IResData>> IEnumerable<KeyValuePair<string, IResData>>.GetEnumerator()
        {
            foreach (Node node in Nodes)
            {
                yield return new KeyValuePair<string, IResData>(node.Key, node.Value);
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator"/> which can be used to iterate over the items in the dictionary.
        /// </summary>
        /// <returns>An enumerator to iterate over the items in the dictionary.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (Node node in Nodes)
            {
                yield return new KeyValuePair<string, IResData>(node.Key, node.Value);
            }
        }

        void IResData.Load(ResFileLoader loader)
        {
            // Read the header.
            uint size = loader.ReadUInt32();
            int numNodes = loader.ReadInt32(); // Excludes root node.

            // Read the nodes including the root node.
            List<Node> nodes = new List<Node>();
            for (; numNodes >= 0; numNodes--)
            {
                nodes.Add(ReadNode(loader));
            }
            _nodes = nodes;
        }

        void IResData.Save(ResFileSaver saver)
        {
            // Update the Patricia trie values in the nodes.
            UpdateNodes();

            // Write header.
            saver.Write(sizeof(uint) * 2 + (_nodes.Count) * Node.SizeInBytes);
            saver.Write(Count);

            // Write nodes.
            int index = -1; // Start at -1 due to root node.
            foreach (Node node in _nodes)
            {
                saver.Write(node.Reference);
                saver.Write(node.IdxLeft);
                saver.Write(node.IdxRight);
                saver.SaveString(node.Key);
                switch (node.Value)
                {
                    case ResString resString:
                        saver.SaveString(resString);
                        break;
                    default:
                        saver.Save(node.Value, index++);
                        break;
                }
            }
        }

        // ---- METHODS (INTERNAL) -------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the <see cref="IResData"/> instance of the node with the given <paramref name="name"/> using the
        /// Patricia trie logic.
        /// </summary>
        /// <remarks>Nodes are looked up linearly by iterating over the node list internally, this method has been
        /// implemented for test and validation purposes only.</remarks>
        /// <param name="name">The name of the node to look up.</param>
        /// <returns>The <see cref="IResData"/> instance referenced by the found node.</returns>
        internal IResData Traverse(string name)
        {
            Node parent = _nodes[0];
            Node child = _nodes[parent.IdxLeft];
            while (parent.Reference > child.Reference)
            {
                parent = child;
                // Follow the right leaf if the bit is 1, otherwise traverse left.
                child = GetDirection(name, child.Reference) == 1 ? _nodes[child.IdxRight] : _nodes[child.IdxLeft];
            }
            // Check that the resulting name is the expected one.
            if (name != child.Key)
            {
                throw new ResException($"{nameof(ResDict)} lookup failed; expected \"{name}\", got \"{child.Key}\".");
            }
            return child.Value;
        }

        // ---- METHODS (PROTECTED) ------------------------------------------------------------------------------------
        
        protected abstract IResData LoadNodeValue(ResFileLoader loader);

        // ---- METHODS (PRIVATE) --------------------------------------------------------------------------------------

        private void UpdateNodes()
        {
            // Create a new root node with empty key so the length can be retrieved throughout the process.
            _nodes[0] = new Node() { Key = String.Empty };

            // Update the data-referencing nodes.
            for (ushort i = 1; i < _nodes.Count; i++)
            {
                Node current = _nodes[i];
                string curKey = current.Key;

                // Iterate through the tree to get the string for bit comparison.
                Node parent = _nodes[0];
                Node child = _nodes[parent.IdxLeft];
                while (parent.Reference > child.Reference)
                {
                    parent = child;
                    child = GetDirection(curKey, child.Reference) == 1 ? _nodes[child.IdxRight] : _nodes[child.IdxLeft];
                }
                uint reference = (uint)Math.Max(curKey.Length, child.Key.Length) * 8;
                // Check for duplicate keys.
                while (GetDirection(child.Key, reference) == GetDirection(curKey, reference))
                {
                    if (reference == 0) throw new ResException($"Duplicate key \"{curKey}\" in {this}.");
                    reference--;
                }
                current.Reference = reference;

                // Form the tree structure of the nodes.
                parent = _nodes[0];
                child = _nodes[parent.IdxLeft];
                // Find the node where to insert the current one.
                while (parent.Reference > child.Reference && child.Reference > reference)
                {
                    parent = child;
                    child = GetDirection(curKey, child.Reference) == 1 ? _nodes[child.IdxRight] : _nodes[child.IdxLeft];
                }
                // Attach left or right depending on the resulting direction bit.
                if (GetDirection(curKey, current.Reference) == 1)
                {
                    current.IdxLeft = (ushort)_nodes.IndexOf(child);
                    current.IdxRight = i;
                }
                else
                {
                    current.IdxLeft = i;
                    current.IdxRight = (ushort)_nodes.IndexOf(child);
                }
                // Attach left or right to the parent depending on the resulting parent direction bit.
                if (GetDirection(curKey, parent.Reference) == 1)
                {
                    parent.IdxRight = i;
                }
                else
                {
                    parent.IdxLeft = i;
                }
            }

            // Remove the dummy empty key in the root again.
            _nodes[0].Key = null;
        }

        private int GetDirection(string name, uint reference)
        {
            int walkDirection = (int)(reference >> 3);
            int bitPosition = (int)(reference & 0b00000111);
            return walkDirection < name.Length ? (name[walkDirection] >> bitPosition) & 1 : 0;
        }

        private bool Lookup(int index, out Node node, bool throwOnFail = true)
        {
            if (index < 0 || index > Count)
            {
                if (throwOnFail) throw new IndexOutOfRangeException($"{index} out of bounds in {this}.");
                node = null;
                return false;
            }
            node = _nodes[index + 1];
            return true;
        }

        private bool Lookup(string key, out Node node, out int index, bool throwOnFail = true)
        {
            int i = 0;
            foreach (Node foundNode in Nodes)
            {
                i++;
                if (foundNode.Key == key)
                {
                    node = foundNode;
                    index = i;
                    return true;
                }
            }
            if (throwOnFail) throw new ArgumentException($"{key} not found in {this}.", nameof(key));
            node = null;
            index = -1;
            return false;
        }

        private bool Lookup(IResData value, out Node node, out int index, bool throwOnFail = true)
        {
            int i = 0;
            foreach (Node foundNode in Nodes)
            {
                i++;
                if (foundNode.Value == value)
                {
                    node = foundNode;
                    index = i;
                    return true;
                }
            }
            if (throwOnFail) throw new ArgumentException($"{value} not found in {this}.", nameof(value));
            node = null;
            index = -1;
            return false;
        }

        private Node ReadNode(ResFileLoader loader)
        {
            return new Node()
            {
                Reference = loader.ReadUInt32(),
                IdxLeft = loader.ReadUInt16(),
                IdxRight = loader.ReadUInt16(),
                Key = loader.LoadString(),
                Value = LoadNodeValue(loader)
            };
        }

        // ---- CLASSES ------------------------------------------------------------------------------------------------

        [DebuggerDisplay(nameof(Node) + " {" + nameof(Key) + "}")]
        protected class Node
        {
            internal const int SizeInBytes = 16;

            internal uint Reference;
            internal ushort IdxLeft;
            internal ushort IdxRight;
            internal string Key;
            internal IResData Value;

            internal Node()
            {
                Reference = UInt32.MaxValue;
            }

            internal Node(string key, IResData value) : this()
            {
                Key = key;
                Value = value;
            }
        }

        private class TypeProxy
        {
            private ResDict _dict;

            internal TypeProxy(ResDict dict)
            {
                _dict = dict;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public KeyValuePair<string, IResData>[] Items
            {
                get { return _dict.ToArray(); }
            }
        }
    }

    /// <summary>
    /// Represents a dictionary which can quickly look up <see cref="IResData"/> instances of type
    /// <typeparamref name="T"/> via key or index.
    /// </summary>
    /// <typeparam name="T">The specialized type of the <see cref="IResData"/> instances.</typeparam>
    public sealed class ResDict<T> : ResDict, IEnumerable<KeyValuePair<string, T>>
        where T : IResData, new()
    {
        // ---- CONSTRUCTORS & DESTRUCTOR ------------------------------------------------------------------------------

        public ResDict() : base()
        {
        }

        // ---- PROPERTIES ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets all stored instances.
        /// </summary>
        public new IEnumerable<T> Values
        {
            get
            {
                foreach (Node node in Nodes)
                {
                    yield return (T)node.Value;
                }
            }
        }

        // ---- OPERATORS ----------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the value stored at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The 0-based index of the instance to get or set.</param>
        /// <returns>The instance at the specified <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is smaller than 0 or bigger or equal to
        /// <see cref="Count"/>.</exception>
        public new T this[int index]
        {
            get { return (T)base[index]; }
            set { base[index] = value; }
        }

        /// <summary>
        /// Gets or sets the value stored under the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The textual key of the instance to get or set.</param>
        /// <returns>The instance with the specified <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentException">An instance with the same <paramref name="key"/> already exists.
        /// </exception>
        /// <exception cref="KeyNotFoundException">An instance with the given <paramref name="key"/> does not exist.
        /// </exception>
        public new T this[string key]
        {
            get { return (T)base[key]; }
            set { base[key] = value; }
        }

        // ---- METHODS (PUBLIC) ---------------------------------------------------------------------------------------

        /// <summary>
        /// Adds the given <paramref name="value"/> under the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The textual key under which the <see cref="IResData"/> instance will be stored.</param>
        /// <param name="value">The <see cref="IResData"/> to add.</param>
        /// <exception cref="ArgumentException">An <see cref="IResData"/> instance with the same <paramref name="key"/>
        /// already exists.</exception>
        public void Add(string key, T value)
        {
            Add(key, (IResData)value);
        }

        /// <summary>
        /// Determines whether the given <paramref name="value"/> is in the dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to locate in the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="value"/> was found in the dictionary; otherwise <c>false</c>.
        /// </returns>
        public bool ContainsValue(T value)
        {
            return ContainsValue((IResData)value);
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{KeyValuePair{String, T}}"/> which can be used to iterate over the items in
        /// the dictionary.
        /// </summary>
        /// <returns>An enumerator to iterate over the items in the dictionary.</returns>
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            foreach (Node node in Nodes)
            {
                yield return new KeyValuePair<string, T>(node.Key, (T)node.Value);
            }
        }

        /// <summary>
        /// Searches for the specified <paramref name="value"/> and returns the zero-based index of the first occurrence
        /// within the entire dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to locate in the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns>The zero-based index of the first occurence of <paramref name="value"/> within the entire
        /// dictionary if found; otherwise <c>-1</c>.</returns>
        public int IndexOf(T value)
        {
            return IndexOf((IResData)value);
        }

        /// <summary>
        /// Removes the first occurrence of a specific <paramref name="value"/> from the dictionary.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> instance to remove from the dictionary. The value can be
        /// <c>null</c>.</param>
        /// <returns><c>true</c> if <paramref name="value"/> was successfully removed; otherwise <c>false</c>. This
        /// method also returns <c>false</c> if <paramref name="value"/> was not found in the dictionary.</returns>
        public bool Remove(T value)
        {
            return Remove((IResData)value);
        }

        /// <summary>
        /// Copies the elements of the dictionary as <see cref="KeyValuePair{String, IResDat}"/> instances to a new
        /// array and returns it.
        /// </summary>
        /// <returns>An array containing copies of the elements.</returns>
        public new KeyValuePair<string, T>[] ToArray()
        {
            KeyValuePair<string, T>[] resData = new KeyValuePair<string, T>[Count];
            int i = 0;
            foreach (Node node in Nodes)
            {
                resData[i] = new KeyValuePair<string, T>(node.Key, (T)node.Value);
                i++;
            }
            return resData;
        }

        /// <summary>
        /// Returns <c>true</c> if a key was found for the given <paramref name="value"/> and has been assigned to
        /// <paramref name="key"/>, or <c>false</c> if no key was found for the value and <c>null</c> was assigned to
        /// <paramref name="key"/>.
        /// </summary>
        /// <param name="value">The <see cref="IResData"/> to look up a key for.</param>
        /// <param name="key">The variable receiving the found key or <c>null</c>.</param>
        /// <returns><c>true</c> if a key was found and assigned; otherwise <c>false</c>.</returns>
        public bool TryGetKey(T value, out string key)
        {
            return TryGetKey((IResData)value, out key);
        }
        
        /// <summary>
        /// Returns <c>true</c> if an instance was stored under the given <paramref name="key"/> and has been assigned
        /// to <paramref name="value"/>, or <c>false</c> if no instance is stored under the given <paramref name="key"/>
        /// and <c>null</c> was assigned to <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The textual key of the instance to get or set.</param>
        /// <param name="value">The variable receiving the found instance or <c>null</c>.</param>
        /// <returns><c>true</c> if an instance was found and assigned; otherwise <c>false</c>.</returns>
        public bool TryGetValue(string key, out T value)
        {
            if (TryGetValue(key, out IResData resData))
            {
                value = (T)resData;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        // ---- METHODS (INTERNAL) -------------------------------------------------------------------------------------

        protected override IResData LoadNodeValue(ResFileLoader loader)
        {
            return loader.Load<T>();
        }
    }
}
