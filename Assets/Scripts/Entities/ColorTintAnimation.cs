using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ColorTintAnimation : MonoBehaviour
{
    public MaterialSet[] sets;

    public float defaultFadeOutTime = .5f;
    public float defaultFadeInTime = 0f;
    public Color defaultColor = Color.red;

    private CoroutinePlus currentCoroutine;

    public float TintFactor
    {
        get => _factor;
        set
        { 
            _factor = value; 
            for (int i = 0; i < sets.Length; i++)
                sets[i].instancedMaterial.SetFloat(Tint_Factor_Name, value);
        }
    }
    private float _factor = 0;

    const string Tint_Color_Name = "_TintColor";
    const string Tint_Factor_Name = "_TintFactor";
    const string Fresnel_Name = "_FRESNELACTIVE";

    private void Awake()
    {
        for (int i = 0; i < sets.Length; i++)
        {
            MaterialSet set = sets[i];
            if (set.materialSlot == null)set.materialSlot = set.renderers[0].sharedMaterial;
            set.instancedMaterial = new Material(set.materialSlot);
            for (int i1 = 0; i1 < set.renderers.Length; i1++)
                set.renderers[i1].material = set.instancedMaterial; 
        }
        SetTintColor(defaultColor);
    }

    public void SetTintColor(Color color)
    {
        for (int i = 0; i < sets.Length; i++)
            sets[i].instancedMaterial.SetColor(Tint_Color_Name, color);
    }


    public void BeginAnimation() => BeginAnimation(defaultFadeOutTime, defaultFadeInTime);
    public void BeginAnimation(float fadeOutTime) => BeginAnimation(fadeOutTime, defaultFadeInTime);
    public void BeginAnimation(float fadeOutTime, float fadeInTime) => 
        CoroutinePlus.Begin(ref currentCoroutine, AnimationIEnumerator(fadeOutTime, fadeInTime), this);

    IEnumerator AnimationIEnumerator(float fadeOutTime, float fadeInTime)
    {
        float changeFactor = fadeInTime > 0f ? 1/fadeInTime : -1/fadeOutTime;

        if(changeFactor < 0) TintFactor = 1f;

        while ((changeFactor < 0 && TintFactor > 0)||(changeFactor > 0 && TintFactor < 1))
        {
            TintFactor += changeFactor * Time.deltaTime;
            if (changeFactor > 0 && TintFactor >= 1)
            {
                TintFactor = 1f;
                changeFactor = -1 / fadeOutTime;
            }
            yield return null;
        }
        TintFactor = 0f;
        currentCoroutine = null;
    }

    private void OnDisable()
    {
        CoroutinePlus.Stop(ref currentCoroutine);
        TintFactor = 0f;
    }


    public void SetFresnel(bool value)
    {
        for (int i = 0; i < sets.Length; i++)
            sets[i].instancedMaterial.SetInt(Fresnel_Name, value ? 1 : 0);

        //_FRESNELACTIVE
    }

    [System.Serializable]
    public class MaterialSet
    {
        public Material materialSlot;
        [HideProperty] public Material instancedMaterial;
        public Renderer[] renderers = new Renderer[0];
    }

    [Button]
    protected void AutoSetup()
    {
        List<Material> materialSlots = new();
        sets = new MaterialSet[0];

        for (int i = 0; i < transform.childCount; i++)
        {
            if(transform.GetChild(i).TryGetComponent(out Renderer renderer))
            {
                if(!materialSlots.Contains(renderer.sharedMaterial))
                {
                    materialSlots.Add(renderer.sharedMaterial);
                    sets = sets.Append(new()).ToArray();
                    int slot = materialSlots.Count-1;
                    sets[slot].materialSlot = renderer.sharedMaterial;
                    sets[slot].renderers = sets[slot].renderers.Append(renderer).ToArray();
                }
                else
                {
                    int slot = materialSlots.IndexOf(renderer.sharedMaterial);
                    sets[slot].renderers = sets[slot].renderers.Append(renderer).ToArray();
                }

            }
        }
    }

}
