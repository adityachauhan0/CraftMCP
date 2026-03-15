using Avalonia;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;

namespace CraftMCP.App.ViewModels;

public sealed class LayerItemViewModel
{
    public LayerItemViewModel(
        NodeId nodeId,
        string name,
        NodeKind kind,
        int depth,
        bool isSelected,
        bool isVisible,
        bool isLocked,
        bool canMoveUp,
        bool canMoveDown)
    {
        NodeId = nodeId;
        Name = name;
        Kind = kind;
        Depth = depth;
        IsSelected = isSelected;
        IsVisible = isVisible;
        IsLocked = isLocked;
        CanMoveUp = canMoveUp;
        CanMoveDown = canMoveDown;
    }

    public NodeId NodeId { get; }

    public string NodeIdText => NodeId.Value;

    public string Name { get; }

    public NodeKind Kind { get; }

    public string KindLabel => Kind.ToString();

    public int Depth { get; }

    public Thickness Indent => new(Depth * 16, 0, 0, 0);

    public bool IsSelected { get; }

    public bool IsVisible { get; }

    public bool IsLocked { get; }

    public bool CanMoveUp { get; }

    public bool CanMoveDown { get; }
}
