using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace XMLDoc2Markdown.Utility
{
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class NamespaceDirectoryTree : IEnumerable<KeyValuePair<string, string>>
    {
#region Private members

        [DebuggerDisplay("Value = {" + nameof(Value) + "}; {" + nameof(Children) + "}")]
        private sealed class NamespaceNode : IEnumerable<NamespaceNode>
        {
            public NamespaceNode(string? valueString)
            {
                this.Value = valueString;
                this.Children = new Dictionary<string, NamespaceNode>();
            }

            public bool Ends { get; set; }

            public string? Value { get; }

            public IDictionary<string, NamespaceNode> Children { get; }

            public IEnumerator<NamespaceNode> GetEnumerator()
            {
                var nodeBacklog = new Queue<NamespaceNode>(this.Children.Count);
                nodeBacklog.Enqueue(this);
                while (nodeBacklog.Count != 0)
                {
                    NamespaceNode temp = nodeBacklog.Dequeue();
                    foreach (NamespaceNode child in temp.Children.Values)
                    {
                        nodeBacklog.Enqueue(child);
                    }

                    yield return temp;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public NamespaceNode GetOrAddChild(string path)
            {
                if (path is null || path.Length == 0)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                if (this.Children.TryGetValue(path, out NamespaceNode? child))
                {
                    return child;
                }

                child = new NamespaceNode(path);
                this.Children.Add(child.Value!, child);
                return child;
            }

            public IEnumerable<KeyValuePair<string, string>> EnumerateDirectories()
            {
                if (this.Ends)
                {
                    if (this.Value is null)
                    {
                        yield break;
                    }

                    yield return KeyValuePair.Create(this.Value, this.Value);
                }

                string separator = this.Children.Count == 1 && !this.Ends ? "." : "\\";
                foreach (NamespaceNode child in this.Children.Values)
                {
                    foreach (KeyValuePair<string, string> kvp in child.EnumerateDirectories())
                    {
                        if (this.Value is null)
                        {
                            yield return kvp;
                        }
                        else
                        {
                            yield return KeyValuePair.Create(string.Concat(this.Value, ".", kvp.Value), string.Concat(this.Value, separator, kvp.Key));
                        }
                    }
                }
            }
        }

#endregion

#region Fields

        private readonly NamespaceNode _root = new NamespaceNode(null);

        public int Count => this._root.Count(n => n.Ends);

#endregion

#region Constructors

        public NamespaceDirectoryTree() { }

        public NamespaceDirectoryTree(IEnumerable<string> namespaces)
        {
            foreach (string ns in namespaces)
            {
                this.AddPath(this._root, ns);
            }
        }

#endregion

#region Public members

        private void AddPath(NamespaceNode node, string ns)
        {
            if (ns.Length == 0)
            {
                throw new ArgumentNullException(nameof(ns));
            }

            NamespaceNode parent = node;
            string parentNamespace = ns;
            int index;
            do
            {
                index = parentNamespace.IndexOf('.');
                if (index == -1)
                {
                    NamespaceNode child = parent.GetOrAddChild(parentNamespace);
                    child.Ends = true;
                }
                else
                {
                    NamespaceNode child = parent.GetOrAddChild(parentNamespace.Substring(0, index));
                    parent = child;
                    parentNamespace = parentNamespace.Substring(index + 1);
                }
            } while (index != -1);
        }

        public void Add(string item) => this.AddPath(this._root, item);

        public void Clear() => this._root.Children.Clear();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
            this._root
               .EnumerateDirectories()
               .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

#endregion
    }
}
