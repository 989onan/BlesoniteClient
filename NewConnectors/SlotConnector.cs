#region

using FrooxEngine;
using UnityEngine;
using UnityFrooxEngineRunner;

#endregion

namespace Thundagun.NewConnectors;

[System.Flags]
public enum SlotTransferType
{

    RefID = 1 << 0,
    Destroy = 1 << 1,
    Create = 1 << 2,
    Name = 1 << 3,
    Active = 1 << 4,
    Position = 1 << 5,
    Rotation = 1 << 6,
    Scale = 1 << 7,
    Parent = 1 << 8
}

public class SlotConnector : Connector<Slot>, ISlotConnector
{
    public bool Active;
    public byte ForceLayer;
    public ushort GameObjectRequests;
    public SlotConnector ParentConnector;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public bool ShouldDestroy;
    public Transform Transform;

    public static readonly byte TYPE = 1;

    public string Name = "";
    public string NewName = "";

    public ulong IDposition = 0;

    public void WriteDataToBuffer(SlotTransferType type, bool destroy, bool create)
    {
        try
        {
            type |= SlotTransferType.RefID;
            try
            {
                NewName = this.Owner.Name;
                if (!Name.Equals(NewName)) { type |= SlotTransferType.Name; }
            }
            catch
            {
                //idc
            }
            if (type.HasFlag(SlotTransferType.Destroy))
            {

                type = SlotTransferType.Destroy;
                type |= SlotTransferType.RefID;
            }


            MemoryObjectManagement.Save(TYPE);
            MemoryObjectManagement.Save(((int)type));

            if (type.HasFlag(SlotTransferType.RefID))
            {
                try
                {
                    IDposition = this.Owner.ReferenceID.Position;
                    MemoryObjectManagement.Save(this.Owner.ReferenceID.Position);
                }
                catch
                {
                    ulong refid = 0;
                    MemoryObjectManagement.Save(refid);
                }

            }
            if (type.HasFlag(SlotTransferType.Destroy))
            {

                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(((int)type));
                MemoryObjectManagement.Save(destroy);
                MemoryObjectManagement.Save(IDposition);
                MemoryObjectManagement.Release();
                return;

            }
            if (type.HasFlag(SlotTransferType.Create))
            {
                MemoryObjectManagement.Save(create);
            }






            if (type.HasFlag(SlotTransferType.Name))
            {
                Name = NewName;

                try
                {
                    if (Name.Equals("FINIS_FR"))
                    {
                        Name = "NICE TRY";
                    }
                    MemoryObjectManagement.SaveString(Name);
                }
                catch
                {
                    string name = "UNNAMED";
                    MemoryObjectManagement.SaveString(name);
                }
            }


            if (type.HasFlag(SlotTransferType.Active))
            {
                MemoryObjectManagement.Save(this.Active);
            }
            if (type.HasFlag(SlotTransferType.Position))
            {
                MemoryObjectManagement.Save(Position.x);
                MemoryObjectManagement.Save(Position.y);
                MemoryObjectManagement.Save(Position.z);
            }
            if (type.HasFlag(SlotTransferType.Rotation))
            {
                MemoryObjectManagement.Save(Rotation.x);
                MemoryObjectManagement.Save(Rotation.y);
                MemoryObjectManagement.Save(Rotation.z);
                MemoryObjectManagement.Save(Rotation.w);
            }
            if (type.HasFlag(SlotTransferType.Scale))
            {
                MemoryObjectManagement.Save(Scale.x);
                MemoryObjectManagement.Save(Scale.y);
                MemoryObjectManagement.Save(Scale.z);
            }
            if (type.HasFlag(SlotTransferType.Parent))
            {
                try
                {
                    MemoryObjectManagement.Save(this.ParentConnector.Owner.ReferenceID.Position);
                }
                catch
                {
                    MemoryObjectManagement.Save((ulong)0);
                }
            }
            MemoryObjectManagement.Release();







        }
        catch (System.Exception e)
        {
            Thundagun.Msg(e.Message.ToString());
            Thundagun.Msg(e.StackTrace.ToString());
            MemoryObjectManagement.Purge();
        }
    }


    public WorldConnector WorldConnector => (WorldConnector)World.Connector;

    public GameObject GeneratedGameObject { get; private set; }

    public int Layer => GeneratedGameObject == null ? 0 : GeneratedGameObject.layer;

    public override void Initialize()
    {
        ParentConnector = Owner.Parent?.Connector as SlotConnector;
        Thundagun.QueuePacket(new ApplyChangesSlotConnector(this, !Owner.IsRootSlot));
    }

    public override void ApplyChanges()
    {
        Thundagun.QueuePacket(new ApplyChangesSlotConnector(this));
    }

    public override void Destroy(bool destroyingWorld)
    {
        Thundagun.QueuePacket(new DestroySlotConnector(this, destroyingWorld));
    }

    public static IConnector<Slot> Constructor()
    {
        return new SlotConnector();
    }

    public GameObject ForceGetGameObject()
    {
        if (GeneratedGameObject == null)
            GenerateGameObject();
        return GeneratedGameObject;
    }

    public GameObject RequestGameObject()
    {
        GameObjectRequests++;
        return ForceGetGameObject();
    }

    public void FreeGameObject()
    {
        GameObjectRequests--;
        TryDestroy();
    }

    public void TryDestroy(bool destroyingWorld = false)
    {
        if (!ShouldDestroy || GameObjectRequests != 0)
            return;
        if (!destroyingWorld)
        {
            if (GeneratedGameObject) Object.Destroy(GeneratedGameObject);
            ParentConnector?.FreeGameObject();
        }

        GeneratedGameObject = null;
        Transform = null;
        ParentConnector = null;
        this.WriteDataToBuffer(SlotTransferType.Destroy, true, false);
    }

    private void GenerateGameObject()
    {
        GeneratedGameObject = new GameObject("");
        Transform = GeneratedGameObject.transform;
        UpdateParent();
        UpdateLayer();
        SetData();
        this.WriteDataToBuffer(SlotTransferType.Parent|SlotTransferType.Create | SlotTransferType.Active, false, true);
    }

    private void UpdateParent()
    {
        var gameObject = ParentConnector != null ? ParentConnector.RequestGameObject() : WorldConnector.WorldRoot;
        Transform.SetParent(gameObject.transform, false);
    }

    public void UpdateLayer()
    {
        var layer = ForceLayer <= 0 ? Transform.parent.gameObject.layer : ForceLayer;
        if (layer == GeneratedGameObject.layer)
            return;
        SetHiearchyLayer(GeneratedGameObject, layer);
    }

    public static void SetHiearchyLayer(GameObject root, int layer)
    {
        root.layer = layer;
        for (var index = 0; index < root.transform.childCount; ++index)
            SetHiearchyLayer(root.transform.GetChild(index).gameObject, layer);
    }

    public void SetData()
    {
        GeneratedGameObject.SetActive(Active);
        var transform = Transform;
        transform.localPosition = Position;
        transform.localRotation = Rotation;
        transform.localScale = Scale;
        this.WriteDataToBuffer(SlotTransferType.Rotation | SlotTransferType.Position | SlotTransferType.Scale, false, true);
    }
}

public class ApplyChangesSlotConnector : UpdatePacket<SlotConnector>
{
    public bool Active;
    public bool ActiveChanged;
    public SlotConnector NewParentSlot;
    public Vector3 Position;
    public bool PositionChanged;
    public bool Reparent;
    public Quaternion Rotation;
    public bool RotationChanged;
    public Vector3 Scale;
    public bool ScaleChanged;

    public ApplyChangesSlotConnector(SlotConnector owner, bool forceReparent) : base(owner)
    {
        var o = owner.Owner;
        var parent = o.Parent;
        if ((parent != null && parent.Connector != owner.ParentConnector) || forceReparent)
        {
            Reparent = true;
            NewParentSlot = o.Parent.Connector as SlotConnector;
        }

        ActiveChanged = o.ActiveSelf_Field.GetWasChangedAndClear();
        Active = o.ActiveSelf;
        PositionChanged = o.Position_Field.GetWasChangedAndClear();
        Position = o.Position_Field.Value.ToUnity();
        RotationChanged = o.Rotation_Field.GetWasChangedAndClear();
        Rotation = o.Rotation_Field.Value.ToUnity();
        ScaleChanged = o.Scale_Field.GetWasChangedAndClear();
        Scale = o.Scale_Field.Value.ToUnity();
    }

    public ApplyChangesSlotConnector(SlotConnector owner) : base(owner)
    {
        var o = owner.Owner;
        var parent = o.Parent;
        if (parent?.Connector != owner.ParentConnector && parent != null)
        {
            Reparent = true;
            NewParentSlot = o.Parent.Connector as SlotConnector;
        }

        ActiveChanged = o.ActiveSelf_Field.GetWasChangedAndClear();
        Active = o.ActiveSelf;
        PositionChanged = o.Position_Field.GetWasChangedAndClear();
        Position = o.Position_Field.Value.ToUnity();
        RotationChanged = o.Rotation_Field.GetWasChangedAndClear();
        Rotation = o.Rotation_Field.Value.ToUnity();
        ScaleChanged = o.Scale_Field.GetWasChangedAndClear();
        Scale = o.Scale_Field.Value.ToUnity();
    }

    public override void Update()
    {
        Owner.Active = Active;
        Owner.Position = Position;
        Owner.Rotation = Rotation;
        Owner.Scale = Scale;

        var generatedGameObject = Owner.GeneratedGameObject;
        if (!(generatedGameObject != null))
            return;

        if (Reparent)
        {
            Owner.ParentConnector?.FreeGameObject();
            GameObject gameObject;
            if (NewParentSlot != null)
            {
                Owner.ParentConnector = NewParentSlot;
                gameObject = Owner.ParentConnector.RequestGameObject();
            }
            else
            {
                gameObject = Owner.WorldConnector.WorldRoot;
            }

            Owner.Transform.SetParent(gameObject.transform, false);
        }

        Owner.UpdateLayer();
        Owner.SetData();
    }
}

public class DestroySlotConnector : UpdatePacket<SlotConnector>
{
    public bool DestroyingWorld;

    public DestroySlotConnector(SlotConnector owner, bool destroyingWorld) : base(owner)
    {
        DestroyingWorld = destroyingWorld;
    }

    public override void Update()
    {
        Owner.ShouldDestroy = true;
        Owner.TryDestroy(DestroyingWorld);
    }
}