﻿using UnityEngine;

[DisallowMultipleComponent]
public class MaterialPropertyEditor : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    
    [SerializeField]
    private Color baseColor = Color.white;

    [SerializeField, Range(0f, 1f)] 
    private float alphaCutoff = 0.5f;
    [SerializeField, Range(0f, 1f)] 
    private float metallic = 0f;
    [SerializeField, Range(0f, 1f)] 
    private float smoothness = 0.5f;
        
    static MaterialPropertyBlock block;
        
    void Awake () {
        OnValidate();
    }
    void OnValidate () {
        if (block == null) {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, alphaCutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}