using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Body;

public sealed partial class OrganRelationSystem : EntitySystem
{
    [Dependency] private EntityQuery<ChildOrganComponent> _child;
    [Dependency] private EntityQuery<ParentOrganComponent> _parent;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParentOrganComponent, ComponentShutdown>(OnParentShutdown);
        SubscribeLocalEvent<ChildOrganComponent, ComponentShutdown>(OnChildShutdown);
    }

    private void OnChildShutdown(Entity<ChildOrganComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Parent is not { } parentUid)
            return;

        if (_parent.TryComp(parentUid, out var parentComp))
        {
            parentComp.Children.Remove(ent);
            Dirty(parentUid, parentComp);
        }
    }

    private void OnParentShutdown(Entity<ParentOrganComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Children.Count == 0)
            return;

        foreach (var childUid in ent.Comp.Children)
        {
            if (_child.TryComp(childUid, out var childComp))
            {
                childComp.Parent = null;
                Dirty(childUid, childComp);
            }
        }
    }

    /// <summary>
    /// Associates a parent organ and a child organ.
    /// </summary>
    [PublicAPI]
    public void Relate(Entity<ParentOrganComponent?> parent, Entity<ChildOrganComponent?> child)
    {
        if (!_parent.Resolve(parent, ref parent.Comp) || !_child.Resolve(child, ref child.Comp))
            return;

        if (child.Comp.Parent == parent.Owner)
            return;

        DebugTools.Assert(child.Comp.Parent == null);

        parent.Comp.Children.Add(child);
        Dirty(parent, parent.Comp);

        child.Comp.Parent = parent;
        Dirty(child, child.Comp);
    }

    /// <summary>
    /// Breaks the relationship between a child orphan and its parent.
    /// </summary>
    [PublicAPI]
    public void Orphan(Entity<ChildOrganComponent?> child)
    {
        if (!_child.Resolve(child, ref child.Comp))
            return;

        if (child.Comp.Parent is not { } parentUid)
            return;

        child.Comp.Parent = null;
        Dirty(child, child.Comp);

        if (_parent.TryComp(parentUid, out var parentComp))
        {
            parentComp.Children.Remove(child);
            Dirty(parentUid, parentComp);
        }
    }

    /// <summary>
    /// Enumerates all parents of a child organ recursively.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<ParentOrganComponent>> AllParents(Entity<ChildOrganComponent?> child)
    {
        if (!_child.Resolve(child, ref child.Comp))
            yield break;

        while (child.Comp?.Parent is { } parent)
        {
            if (!_parent.TryComp(parent, out var parentComp))
                yield break;

            yield return (parent, parentComp);

            if (!_child.TryGetComponent(parent, out var parentChild))
                yield break;

            child = (parent, parentChild);
        }
    }

    /// <summary>
    /// Enumerates all children of a parent organ recursively.
    /// </summary>
    [PublicAPI]
    public IEnumerable<Entity<ChildOrganComponent>> AllChildren(Entity<ParentOrganComponent?> parent)
    {
        if (!_parent.Resolve(parent, ref parent.Comp, false))
            yield break;

        foreach (var child in parent.Comp.Children)
        {
            if (!_child.TryComp(child, out var childComp))
                continue;

            yield return (child, childComp);

            foreach (var childChild in AllChildren(child))
            {
                yield return childChild;
            }
        }
    }
}
