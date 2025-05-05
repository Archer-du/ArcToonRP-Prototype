using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class DrawNormalGizmos : MonoBehaviour
{
    public float lineLength = 0.01f;
    private float _lineLengthCache = 0;
    private Mesh _mesh;
    private Mesh _meshCache;
    
    struct NormalLine
    {
        public Vector3 posFrom;
        public Vector3 posTo;
    }

    private List<NormalLine> _normalLines;
    private bool _isDrawGizmos;

    void CalculateNormalLine()
    {
        _normalLines.Clear();
        if (_mesh != null)
        {
            var matrix = transform.localToWorldMatrix;
            Vector3[] normals = _mesh.normals;
            int normalsLength = normals.Length;
            Color[] colors = _mesh.colors;
            
            for (int i = 0; i < normalsLength; i++)
            {
                var normalLine = new NormalLine();
                Vector3[] vertices = _mesh.vertices;
                Vector3 pos = vertices[i];
                normalLine.posFrom = matrix.MultiplyPoint(pos);
                
                Vector3 normal = normals[i];
                normalLine.posTo = matrix.MultiplyPoint(pos + normal / normal.magnitude * lineLength);

                _normalLines.Add(normalLine);
            }
        }
        _lineLengthCache = lineLength;
        _meshCache = _mesh;
    }
    void OnEnable()
    {
        _normalLines = new List<NormalLine>();
        if(TryGetComponent<MeshFilter>(out MeshFilter filter))
            _mesh = filter.sharedMesh;
        _isDrawGizmos = true;
    }

    private void OnDisable()
    {
        _mesh = null;
        _meshCache = null;
        _normalLines = null;
        _isDrawGizmos = false;
    }

    private void OnDrawGizmos()
    {
        if (!_isDrawGizmos) return;
        if (Math.Abs(lineLength - _lineLengthCache) > 0 || _mesh != _meshCache)
            CalculateNormalLine();
        
        Gizmos.color = Color.magenta;
        if (_mesh != null)
        {
            foreach (var normalLine in _normalLines)
            {
                Gizmos.DrawLine(normalLine.posFrom, normalLine.posTo);
            }
        }
    }
}