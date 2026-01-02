using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public static class InternalShaderHelpers
    {
        public static ShaderTagId ShaderTagId([CallerMemberName] string name = null)
        {
            return new ShaderTagId(name);
        }

        public static int ShaderPropertyID([CallerMemberName] string name = null)
        {
            return Shader.PropertyToID("_" + name);
        }
        
        public static class Path
        {
            public static readonly string CameraCopy = "Hidden/ArcToon/Camera Copy";
            public static readonly string CameraDebug = "Hidden/ArcToon/Camera Debug";
        }
        
        public static class PropertyID
        {
            public static readonly int CameraBufferSize = ShaderPropertyID();
        }
        
        public static class TagId
        {
        }
    }
}