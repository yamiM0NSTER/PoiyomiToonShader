#ifndef POI_VERTEX_MANIPULATION
    #define POI_VERTEX_MANIPULATION
    
    float4 _VertexManipulationLocalTranslation;
    float4 _VertexManipulationLocalRotation;
    float4 _VertexManipulationLocalScale;
    float4 _VertexManipulationWorldTranslation;
    
    float _VertexManipulationHeight;
    float _VertexManipulationHeightBias;
    sampler2D _VertexManipulationHeightMask; float4 _VertexManipulationHeightMask_ST;
    float2 _VertexManipulationHeightPan;
    void applyLocalVertexTransformation(inout float3 normal, inout float4 tangent, inout float4 vertex)
    {
        normal = rotate_with_quaternion(normal, _VertexManipulationLocalRotation);
        tangent.xyz = rotate_with_quaternion(tangent.xyz, _VertexManipulationLocalRotation);
        vertex = transform(vertex, _VertexManipulationLocalTranslation, _VertexManipulationLocalRotation, _VertexManipulationLocalScale);
    }
    
    void applyWorldVertexTransformation(inout float4 worldPos, inout float4 localPos, inout float3 worldNormal, float2 uv)
    {
        
        float3 heightOffset = (tex2Dlod(_VertexManipulationHeightMask, float4(TRANSFORM_TEX(uv, _VertexManipulationHeightMask) + _VertexManipulationHeightPan * _Time.x, 0, 0)).r - _VertexManipulationHeightBias) * _VertexManipulationHeight * worldNormal;
        worldPos.rgb += _VertexManipulationWorldTranslation.xyz * _VertexManipulationWorldTranslation.w + heightOffset;
        localPos.xyz = mul(unity_WorldToObject, worldPos);
    }
#endif
//