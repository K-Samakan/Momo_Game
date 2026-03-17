void ComputeWorldSpacePosition_float(float2 UV, float Depth, out float3 WorldPos)
{
    float2 ndc = UV * 2.0 - 1.0;
    float4 clipPos = float4(ndc, Depth, 1.0);

    // Ya no redefinimos nada, usamos directamente la matriz global
    float4 worldPos = mul(UNITY_MATRIX_I_VP, clipPos); // <- Esta es la correcta en HDRP 17
    WorldPos = worldPos.xyz / worldPos.w;
}
