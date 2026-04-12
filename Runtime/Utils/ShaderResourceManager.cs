using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Utils
{
    public class ShaderResourceManager
    {
        private static ShaderResourceManager instance;
        private static readonly object lockObject = new object();
        
        public static ShaderResourceManager Instance
        {
            get
            {
                lock (lockObject)
                {
                    instance ??= new ShaderResourceManager();
                    return instance;
                }
            }
        }
        
        private readonly Dictionary<string, Material> managedMaterials = new();
        private bool isInitialized = false;
        
        private ShaderResourceManager()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            if (!isInitialized)
            {
                Application.quitting += OnApplicationQuit;
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                #endif
                isInitialized = true;
            }
        }
        
        #if UNITY_EDITOR
        private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                DisposeTransientMaterials();
            }
        }
        #endif
        
        public Material AcquireEngineMaterial(string shaderName)
        {
            lock (managedMaterials)
            {
                if (managedMaterials.TryGetValue(shaderName, out Material existingMaterial))
                {
                    return existingMaterial;
                }
                
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError($"InternalShaderHelpers: Cannot Find Shader: '{shaderName}'");
                    return null;
                }
                
                var material = CoreUtils.CreateEngineMaterial(shader);
                managedMaterials[shaderName] = material;
                Debug.Log($"Create Engine Material: '{shaderName}'");
                
                return material;
            }
        }
        
        public static Material AcquireTransientMaterial(string shaderName)
        {
            return Instance.AcquireEngineMaterial(shaderName);
        }

        public void DisposeTransientMaterials()
        {
            lock (managedMaterials)
            {
                foreach (var kvp in managedMaterials)
                {
                    if (kvp.Value != null)
                    {
                        Debug.Log($"Destroy Engine Material: '{kvp.Key}'");
                        CoreUtils.Destroy(kvp.Value);
                    }
                }
                managedMaterials.Clear();
            }
        }
        
        private void OnApplicationQuit()
        {
            DisposeTransientMaterials();
            
            Application.quitting -= OnApplicationQuit;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            #endif
            instance = null;
        }

    }
}
