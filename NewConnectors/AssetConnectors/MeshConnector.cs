#region

using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using UnityEngine;
using System.Linq;
using UnityFrooxEngineRunner;
using Mesh = UnityEngine.Mesh;

#endregion

namespace Thundagun.NewConnectors.AssetConnectors;
public enum MeshUploadBlenderHint
{
    Vertices = 1 << 0,
    Triangles = 1 << 1,
    BoneID = 1 << 2,
    BoneIndices = 1 << 3,
    BoneWeights = 1 << 4,
    BoneMatrix_Position = 1 << 5,
    BoneMatrix_Rotation = 1 << 6
}

public class MeshConnector : AssetConnector, IMeshConnector
{

    public static volatile int MeshDataCount;
    private BoundingBox _bounds;
    private UnityEngine.Mesh _mesh;
    private UnityMeshData _meshGenData;
    private AssetIntegrated _onLoaded;
    private MeshUploadHint _uploadHint;

    public Mesh Mesh { get; private set; }
    public static readonly byte TYPE = 2;

    public MeshX savedMesh = null;

    public bool upload = false;

    public void WriteDataToBuffer(bool destroy, bool create, ulong meshid, ulong slotrefid, ulong[] bones)
    {

        try
        {
            //int count = meshx.RawPositions.Count();
            //var positions = new float[3 * count];
            //for (int i = 0; i < count; i++)
            //{
            //    positions[3 * i] = meshx.RawPositions[i].x;
            //    positions[3 * i + 1] = meshx.RawPositions[i].z;
            //    positions[3 * i + 2] = meshx.RawPositions[i].y;
            //}
            //meshx.TrimUVChannels();


            //int submesh_amount = meshx.SubmeshCount;


            /*MemoryObjectManagement.Save(submesh_amount);
            for (int i=0; i<submesh_amount; i++)
            {
                var submesh = meshx.GetSubmesh(i);
                MemoryObjectManagement.SaveArray(submesh.RawIndicies);
            }*/

            //
            if (savedMesh.HasBoneBindings)
            {
                if (bones.Length == 0)
                {
                    return;
                }
            }

            if (upload)
            {
                this.upload = false;
                MemoryObjectManagement.Save(TYPE);
                MemoryObjectManagement.Save(slotrefid);
                MemoryObjectManagement.Save(meshid);
                MemoryObjectManagement.Save((int)(MeshUploadBlenderHint.Vertices | MeshUploadBlenderHint.Triangles));
                float[] positions = new float[savedMesh.Vertices.Count() * 3];
                int j = 0;
                foreach (Vertex vert in savedMesh.Vertices)
                {
                    positions[3 * j] = vert.Position.x;
                    positions[3 * j + 1] = vert.Position.y;
                    positions[3 * j + 2] = vert.Position.z;
                    j++;
                }
                MemoryObjectManagement.SaveArray(positions);

                int[] tris = new int[savedMesh.Triangles.Count() * 3];
                j = 0;
                foreach(Triangle tri in savedMesh.Triangles)
                {
                    tris[3 * j] = tri.Vertex0Index;
                    tris[3 * j + 1] = tri.Vertex1Index;
                    tris[3 * j + 2] = tri.Vertex2Index;
                    j++;
                }
                MemoryObjectManagement.SaveArray(tris);
                MemoryObjectManagement.Release();


                if (savedMesh.HasBoneBindings)
                {
                    MemoryObjectManagement.Save(TYPE);
                    MemoryObjectManagement.Save(slotrefid);
                    MemoryObjectManagement.Save(meshid);
                    MemoryObjectManagement.Save((int)(MeshUploadBlenderHint.BoneID| MeshUploadBlenderHint.BoneIndices| MeshUploadBlenderHint.BoneWeights| MeshUploadBlenderHint.BoneMatrix_Position| MeshUploadBlenderHint.BoneMatrix_Rotation));
                    MemoryObjectManagement.SaveArray(bones);
                    Thundagun.Msg("BONES LENGTH IS:" + bones.Length.ToString());
                    int[] boneindices = new int[savedMesh.Vertices.Count() * 4];

                    j = 0;
                    foreach (BoneBinding o in savedMesh.RawBoneBindings)
                    {
                        boneindices[4 * j] = o.boneIndex0;
                        boneindices[4 * j + 1] = o.boneIndex1;
                        boneindices[4 * j + 2] = o.boneIndex2;
                        boneindices[4 * j + 3] = o.boneIndex3;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(boneindices);

                    float[] boneweights = new float[savedMesh.Vertices.Count() * 4];

                    j = 0;
                    foreach (BoneBinding o in savedMesh.RawBoneBindings)
                    {
                        boneweights[4 * j] = o.weight0;
                        boneweights[4 * j + 1] = o.weight1;
                        boneweights[4 * j + 2] = o.weight2;
                        boneweights[4 * j + 3] = o.weight3;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(boneweights);
                    float[] bone_pos = new float[savedMesh.BoneCount * 3];

                    j = 0;
                    foreach (Bone o in savedMesh.Bones)
                    {
                        bone_pos[3 * j] = o.BindPose.Inverse.DecomposedPosition.x;

                        bone_pos[3 * j + 1] = o.BindPose.Inverse.DecomposedPosition.y;
                        bone_pos[3 * j + 2] = o.BindPose.Inverse.DecomposedPosition.z;
                        j++;
                    }
                    MemoryObjectManagement.SaveArray(bone_pos);

                    float[] bone_vector = new float[savedMesh.BoneCount * 3];

                    j = 0;
                    foreach (Bone o in savedMesh.Bones)
                    {
                        float3 pointing = o.BindPose.Inverse.DecomposedRotation  * new float3(0, 0, 1);
                        bone_vector[3 * j] = pointing.x;
                        bone_vector[3 * j + 1] = pointing.y;
                        bone_vector[3 * j + 2] = pointing.z;
                        j++;
                    }

                    MemoryObjectManagement.SaveArray(bone_vector);
                    MemoryObjectManagement.Release();
                    
                }
                
            }




            //TODO: IMPLEMENT THIS - @989onan
            //MemoryObjectManagement.Save(bones != null ? savedMesh.BoneCount : 0);
            //savedMesh.Bones.ToList().ForEach(bone => MemoryObjectManagement.SaveString(bone.Name));
            





            //TODO: Shove this in via an array of arrays and then decode and numpy shape it on the Python side. who cares!
            //List<int> indices = new List<int>();
            //indices.AddRange();// np.reshape(np.array()[:], (submesh.Count, submesh.IndiciesPerElement))
            //for (int i = 0; i < meshx.SubmeshCount; i++)
            //{
            //    submesh = meshx.GetSubmesh(i);
            //    /indices =  (, np.reshape(np.array()[:submesh.Count * submesh.IndiciesPerElement], (submesh.Count, submesh.IndiciesPerElement))), axis = 0)
            //}


            //mesh_data.from_pydata(np.reshape(np.array(v[3]), (-1, 3)), [], indices)
            //mesh_finishes.append(v[2])
        }
        catch (System.Exception e)
        {
            Thundagun.Msg(e.Message.ToString());
            Thundagun.Msg(e.StackTrace.ToString());
            MemoryObjectManagement.Purge();
            this.upload = false;
        }
    }

    public void UpdateMeshData(
        MeshX meshx,
        MeshUploadHint uploadHint,
        BoundingBox bounds,
        AssetIntegrated onLoaded)
    {
        var data = new UnityMeshData();

        savedMesh = meshx;
        if (uploadHint[MeshUploadHint.Flag.Geometry])
        {
            this.upload = true;
        }
        meshx.GenerateUnityMeshData(ref data, ref uploadHint, Engine.SystemInfo);
        UnityAssetIntegrator.EnqueueProcessing(() => Upload2(data, uploadHint, bounds, onLoaded),
            Asset.HighPriorityIntegration);
    }

    public override void Unload()
    {
        UnityAssetIntegrator.EnqueueProcessing(Destroy, true);
    }

    private void Upload2(UnityMeshData data, MeshUploadHint hint, BoundingBox bounds, AssetIntegrated onLoaded)
    {
        if (data == null)
            return;
        if (Mesh != null && !Mesh.isReadable)
        {
            if (Mesh)
                Object.Destroy(Mesh);
            Mesh = null;
        }

        var environmentInstanceChanged = false;
        if (Mesh == null)
        {
            Mesh = new Mesh();
            environmentInstanceChanged = true;
            if (hint[MeshUploadHint.Flag.Dynamic])
                Mesh.MarkDynamic();
        }

        data.Assign(Mesh, hint);

        Mesh.bounds = bounds.ToUnity();
        Mesh.UploadMeshData(!hint[MeshUploadHint.Flag.Readable]);
        if (hint[MeshUploadHint.Flag.Dynamic])
        {
            _meshGenData = data;
            _uploadHint = hint;
            _bounds = bounds;
            _onLoaded = onLoaded;
        }

        onLoaded(environmentInstanceChanged);
        Engine.MeshUpdated();
    }

    private void Upload()
    {
        if (_meshGenData == null)
            return;
        if (Mesh != null && !Mesh.isReadable)
        {
            if (Mesh)
                Object.Destroy(Mesh);
            Mesh = null;
        }

        var environmentInstanceChanged = false;
        if (Mesh == null)
        {
            Mesh = new Mesh();
            environmentInstanceChanged = true;
            if (_uploadHint[MeshUploadHint.Flag.Dynamic])
                Mesh.MarkDynamic();
        }

        _meshGenData.Assign(Mesh, _uploadHint);

        Mesh.bounds = _bounds.ToUnity();
        Mesh.UploadMeshData(!_uploadHint[MeshUploadHint.Flag.Readable]);
        if (!_uploadHint[MeshUploadHint.Flag.Dynamic])
            _meshGenData = null;
        _onLoaded(environmentInstanceChanged);
        _onLoaded = null;
        Engine.MeshUpdated();
    }

    private void Destroy()
    {
        if (Mesh != null)
            Object.Destroy(Mesh);
        Mesh = null;
        _meshGenData = null;
    }
}