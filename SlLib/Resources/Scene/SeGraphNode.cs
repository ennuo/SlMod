using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeGraphNode : SeNodeBase
{
    /// <summary>
    ///     The parent of this node.
    /// </summary>
    public SeGraphNode? Parent
    {
        get => _parent;
        set
        {
            if (_parent == value) return;
            
            // Make sure to fixup the hierarchy since we're changing parents
            if (_parent != null)
            {
                // Fixup parent's first child if appropriate
                if (this == _parent.FirstChild)
                    _parent.FirstChild = NextSibling;
                
                // Maintain sibling links
                if (PrevSibling != null)
                    PrevSibling.NextSibling = NextSibling;
                if (NextSibling != null)
                    NextSibling.PrevSibling = PrevSibling;
                
                PrevSibling = null;
                NextSibling = null;
            }
            
            _parent = value;
            if (value != null)
            {
                // Attach ourselves to the last child of the parent,
                // just for maintaining insertion order in a sense
                SeGraphNode? lastChild = value.FirstChild;
                while (lastChild is { NextSibling: not null })
                    lastChild = lastChild.NextSibling;

                if (lastChild != null)
                {
                    lastChild.NextSibling = this;
                    PrevSibling = lastChild;
                }
                else value.FirstChild = this;
            }
        }
    }
    private SeGraphNode? _parent;
    
    /// <summary>
    ///     The first child of this node.
    /// </summary>
    public SeGraphNode? FirstChild { get; private set; }
    
    /// <summary>
    ///     The previous sibling of this node.
    /// </summary>
    public SeGraphNode? PrevSibling { get; private set; }
    
    /// <summary>
    ///     The next sibling of this node.
    /// </summary>
    public SeGraphNode? NextSibling { get; private set; }
    
    /// <summary>
    ///     Finds the first node in the hierachy that matches a partial name.
    /// </summary>
    /// <param name="name">Partial name to match</param>
    /// <returns>Node that matches partial name, if found</returns>
    public T? FindFirstDescendentThatDerivesFrom<T>() where T : SeGraphNode
    {
        SeGraphNode? child = FirstChild;
        while (child != null)
        {
            if (child is T node)
                return node;
            
            var match = child.FindFirstDescendentThatDerivesFrom<T>();
            if (match != null)
                return match;
            
            child = child.NextSibling;
        }

        return null;
    }
    
    /// <summary>
    ///     Finds the first node in the hierachy that matches a partial name.
    /// </summary>
    /// <param name="name">Partial name to match</param>
    /// <returns>Node that matches partial name, if found</returns>
    public SeGraphNode? FindFirstDescendentByPartialName(string name)
    {
        SeGraphNode? child = FirstChild;
        while (child != null)
        {
            if (child.UidName.Contains(name))
                return child;

            SeGraphNode? match = child.FindFirstDescendentByPartialName(name);
            if (match != null)
                return match;
            
            child = child.NextSibling;
        }

        return null;
    }
    
    /// <summary>
    ///     Traverses node hierarchy for all nodes that derive from a given type.
    /// </summary>
    /// <typeparam name="T">Type of node, must extend SeGraphNode</typeparam>
    /// <returns>List of nodes that derive from type</returns>
    public List<T> FindDescendantsThatDeriveFrom<T>() where T : SeGraphNode
    {
        List<T> nodes = [];
        VisitDescendantsThatDeriveFrom(nodes);
        return nodes;
    }
    
    /// <summary>
    ///     Finds an ancestor node that derives from a given type.
    /// </summary>
    /// <typeparam name="T">Type of node, must extend SeGraphNode</typeparam>
    /// <returns>Ancestor node that derives from type, if found</returns>
    public T? FindAncestorThatDerivesFrom<T>() where T : SeGraphNode
    {
        SeGraphNode? parent = _parent;
        while (parent != null)
        {
            if (parent is T node)
                return node;
            
            parent = parent._parent;
        }
        
        return null;
    }

    public bool IsEnabled()
    {
        SeGraphNode? node = this;
        while (node != null)
        {
            if ((node.BaseFlags & 1) == 0)
                return false;
            node = node._parent;
        }

        return true;
    }
    
    public bool IsVisible()
    {
        if (!IsEnabled()) return false;
        
        SeGraphNode? node = this;
        while (node != null)
        {
            if ((node.BaseFlags & 2) == 0)
                return false;
            node = node._parent;
        }
        
        return true;
    }
    
    /// <summary>
    ///     Loads this graph node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected new int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);

        // So the actual structure is...
        // SePtr<SeGraphNode> Parent @ 0x0
        // SePtr<SeGraphNode> Child @ 0x8
        // SePtr<SeGraphNode> PrevSibling @ 0x10
        // SePtr<SeGraphNode> NextSibling @ 0x18
        // SePtr<SeGraphNode> EditParent(?) @ 0x20

        // ...but in serialized form, it only has a reference to the
        // UID of the parent node.
        int address = context.ReadInt32(offset);
        if (address != 0)
            Parent = context.LoadNode(context.ReadInt32(address));
        
        return offset + 0x28;
    }
    
    private void VisitDescendantsThatDeriveFrom<T>(List<T> nodes) where T : SeGraphNode
    {
        SeGraphNode? child = FirstChild;
        while (child != null)
        {
            if (child is T node)
                nodes.Add(node);
            child.VisitDescendantsThatDeriveFrom(nodes);
            child = child.NextSibling;
        }
    }
}