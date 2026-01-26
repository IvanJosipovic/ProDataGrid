using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class ResourceTreeNodeCollection : IAvaloniaReadOnlyList<ResourceTreeNode>, IList, IDisposable
    {
        private sealed class EmptyResourceTreeNodeCollection : ResourceTreeNodeCollection
        {
            public EmptyResourceTreeNodeCollection() : base(default!)
            {
            }

            protected override void Initialize(AvaloniaList<ResourceTreeNode> nodes)
            {
            }
        }

        internal static readonly ResourceTreeNodeCollection Empty = new EmptyResourceTreeNodeCollection();

        private AvaloniaList<ResourceTreeNode>? _inner;

        protected ResourceTreeNodeCollection(ResourceTreeNode owner)
        {
            Owner = owner;
        }

        public ResourceTreeNode this[int index] => EnsureInitialized()[index];

        public int Count => EnsureInitialized().Count;

        protected ResourceTreeNode Owner { get; }

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        object? IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => EnsureInitialized().CollectionChanged += value;
            remove => EnsureInitialized().CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => EnsureInitialized().PropertyChanged += value;
            remove => EnsureInitialized().PropertyChanged -= value;
        }

        public virtual void Dispose()
        {
            if (_inner is object)
            {
                foreach (var node in _inner)
                {
                    node.Dispose();
                }
            }
        }

        public IEnumerator<ResourceTreeNode> GetEnumerator()
        {
            return EnsureInitialized().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract void Initialize(AvaloniaList<ResourceTreeNode> nodes);

        private AvaloniaList<ResourceTreeNode> EnsureInitialized()
        {
            if (_inner is null)
            {
                _inner = new AvaloniaList<ResourceTreeNode>();
                Initialize(_inner);
            }

            return _inner;
        }

        int IList.Add(object? value) => throw new NotImplementedException();
        void IList.Clear() => throw new NotImplementedException();
        bool IList.Contains(object? value) => EnsureInitialized().Contains((ResourceTreeNode)value!);
        int IList.IndexOf(object? value) => EnsureInitialized().IndexOf((ResourceTreeNode)value!);
        void IList.Insert(int index, object? value) => throw new NotImplementedException();
        void IList.Remove(object? value) => throw new NotImplementedException();
        void IList.RemoveAt(int index) => throw new NotImplementedException();
        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();
    }
}
