using UnityEngine;
using UnityEngine.Rendering;   // CommandBuffer

[RequireComponent(typeof(Renderer), typeof(MeshFilter))]
public class Paintable : MonoBehaviour
{
    [Tooltip("Mask resolution. 512 for props, 1024 for hero objects.")]
    public int textureSize = 1024;

    public RenderTexture PaintMask { get; private set; }
    RenderTexture _temp;
    Material _brushMat;
    Renderer _renderer;
    Mesh _mesh;
    CommandBuffer _cmd;

    static readonly int MainTexID    = Shader.PropertyToID("_MainTex");
    static readonly int PaintMaskID  = Shader.PropertyToID("_PaintMask");
    static readonly int BrushColorID = Shader.PropertyToID("_BrushColor");
    static readonly int BrushPosID   = Shader.PropertyToID("_BrushWorldPos");
    static readonly int BrushDirID   = Shader.PropertyToID("_BrushDir");
    static readonly int BrushSizeID  = Shader.PropertyToID("_BrushSize");
    static readonly int BrushHardID  = Shader.PropertyToID("_BrushHardness");
    static readonly int BrushFlowID  = Shader.PropertyToID("_BrushFlow");
    static readonly int ObjToWorldID = Shader.PropertyToID("_ObjToWorld");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mesh     = GetComponent<MeshFilter>().sharedMesh;

        PaintMask = NewRT();
        _temp     = NewRT();
        Clear(PaintMask);
        Clear(_temp);

        _brushMat = new Material(Shader.Find("Custom/SprayBrush"));
        _cmd = new CommandBuffer { name = "SprayPaint" };

        _renderer.material.SetTexture(PaintMaskID, PaintMask);
    }

    RenderTexture NewRT()
    {
        // Linear read/write: URP is a linear-color-space pipeline.
        var rt = new RenderTexture(textureSize, textureSize, 0,
                                   RenderTextureFormat.ARGB32,
                                   RenderTextureReadWrite.Linear);
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }

    void Clear(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = prev;
    }

    public void PaintAt(Vector3 worldPos, Vector3 worldDir, Color color,
                        float size, float hardness, float flow)
    {
        _brushMat.SetColor(BrushColorID, color);
        _brushMat.SetVector(BrushPosID, worldPos);
        _brushMat.SetVector(BrushDirID, worldDir.normalized);
        _brushMat.SetFloat(BrushSizeID, size);
        _brushMat.SetFloat(BrushHardID, hardness);
        _brushMat.SetFloat(BrushFlowID, flow);
        _brushMat.SetMatrix(ObjToWorldID, transform.localToWorldMatrix);
        _brushMat.SetTexture(MainTexID, PaintMask);    // read current paint

        _cmd.Clear();
        _cmd.SetRenderTarget(_temp);
        _cmd.ClearRenderTarget(true, true, Color.clear);
        for (int s = 0; s < _mesh.subMeshCount; s++)
            _cmd.DrawMesh(_mesh, Matrix4x4.identity, _brushMat, s, 0);
        Graphics.ExecuteCommandBuffer(_cmd);           // runs immediately

        (PaintMask, _temp) = (_temp, PaintMask);       // swap
        _renderer.material.SetTexture(PaintMaskID, PaintMask);
    }

    void OnDestroy()
    {
        if (PaintMask) PaintMask.Release();
        if (_temp) _temp.Release();
        _cmd?.Release();
        if (_brushMat) Destroy(_brushMat);
    }
}