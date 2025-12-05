using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/UV Image (TwoTexture)")]
public class Image_TwoTexMul : Image
{
    [SerializeField]
    private Vector4 _mainTexUV = new Vector4(1, 1, 0, 0);
    [SerializeField]
    private Vector4 _alphaTexUV = new Vector4(1, 1, 0, 0);

    public Vector4 MainTexUV
    {
        get => _mainTexUV;
        set
        {
            if (_mainTexUV != value)
            {
                _mainTexUV = value;
                SetMaterialDirty();
            }
        }
    }

    public Vector4 AlphaTexUV
    {
        get => _alphaTexUV;
        set
        {
            if (_alphaTexUV != value)
            {
                _alphaTexUV = value;
                SetMaterialDirty();
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        ApplyUV();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyUV();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (!isActiveAndEnabled) return;
        ApplyUV();
    }
#endif


    public override void SetMaterialDirty()
    {
        ApplyUV();
        base.SetMaterialDirty();
    }

    protected override void OnDidApplyAnimationProperties()
    {
        SetMaterialDirty();
        base.OnDidApplyAnimationProperties();
    }

    void ApplyUV()
    {
        if (materialForRendering == null) return;
        materialForRendering.SetVector("_MainTexUV", MainTexUV);
        materialForRendering.SetVector("_AlphaTexUV", AlphaTexUV);
    }
}
