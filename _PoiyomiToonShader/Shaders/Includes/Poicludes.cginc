#ifndef POICLUDES
    // Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
    #pragma exclude_renderers d3d11
    #define POICLUDES
    
    UNITY_DECLARE_TEX2D(_MainTex); float4 _MainTex_ST; float4 _MainTex_TexelSize;
    
    float _Clip;
    
    //Structs
    struct appdata
    {
        float4 vertex: POSITION;
        float3 normal: NORMAL;
        float4 tangent: TANGENT;
        float4 color: COLOR;
        float2 uv0: TEXCOORD0;
        float2 uv1: TEXCOORD1;
        float2 uv2: TEXCOORD2;
        float2 uv3: TEXCOORD3;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct v2f
    {
        float4 pos: SV_POSITION;
        float4 uv0: TEXCOORD1;
        float4 uv1: TEXCOORD2;
        float3 normal: TEXCOORD3;
        float3 tangentViewDir: TEXCOORD4;
        float4 tangent: TEXCOORD5;
        float4 worldPos: TEXCOORD6;
        float4 localPos: TEXCOORD7;
        float4 screenPos: TEXCOORD8;
        float4 grabPos: TEXCOORD9;
        #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
            float4 lightmapUV: TEXCOORD10;
        #endif
        float3 modelPos: TEXCOORD11;
        float angleAlpha: TEXCOORD12;
        #if defined(VERTEXLIGHT_ON)
            float3 vertexLightColor: TEXCOORD13;
        #endif
        float4 vertexColor: TEXCOORD14;
        UNITY_SHADOW_COORDS(15)
        UNITY_FOG_COORDS(16)
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };
    
    #ifdef OUTLINE
        float _LineWidth;
        float _OutlineEmission;
        float4 _LineColor;
        sampler2D _OutlineTexture; float4 _OutlineTexture_ST;
        float4 _OutlineTexturePan;
        float4 _OutlineFadeDistance;
        float4 _OutlineGlobalPan;
    #endif
    
    struct PoiLighting
    {
        half3 direction;
        half3 color;
        half attenuation;
        half3 directLighting;
        half3 indirectLighting;
        half lightMap;
        half3 rampedLightMap;
        half3 finalLighting;
        half3 halfDir;
        #if defined(VERTEXLIGHT_ON)
            half3 vertexLightColor;
        #endif
        half nDotL;
        half nDotH;
        half lDotv;
        half lDotH;
        half nDotV;
        half vNDotV;
        half diffuseTerm;
    };
    
    struct PoiCamera
    {
        half3 viewDir;
        half3 tangentViewDir;
        half3 forwardDir;
        half3 worldPos;
        float viewDotNormal;
        float distanceToModel;
        float distanceToVert;
        float3 reflectionDir;
        float3 vertexReflectionDir;
        float2 screenUV;
        float4 screenPos;
        float4 grabPos;
    };
    
    struct PoiMesh
    {
        float3 normals[2];
        float3 tangent;
        float3 bitangent;
        float3 localPos;
        float3 worldPos;
        float3 modelPos;
        float3 tangentSpaceNormal;
        float2 uv[4];
        float4 vertexColor;
        #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
            float4 lightmapUV;
        #endif
        float isFrontFace;
    };
    
    struct PoiTangentData
    {
        float3x3 tangentTransform;
        float3x3 tangentToWorld;
    };
    
    struct FragmentCommonData
    {
        half3 diffColor, specColor;
        // Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
        // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
        half oneMinusReflectivity, smoothness;
        float3 normalWorld;
        float3 eyeVec;
        half alpha;
        float3 posWorld;
        
        #if UNITY_STANDARD_SIMPLE
            half3 reflUVW;
        #endif
        
        #if UNITY_STANDARD_SIMPLE
            half3 tangentSpaceNormal;
        #endif
    };
    
    static PoiLighting poiLight;
    static PoiCamera poiCam;
    static PoiMesh poiMesh;
    static UnityGI gi;
    static FragmentCommonData s;
    static PoiTangentData poiTData;
    float4 finalColor;
    float3 finalEmission;
    float4 mainTexture;
    float4 albedo;
    #define pi float(3.14159265359)
    
#endif
