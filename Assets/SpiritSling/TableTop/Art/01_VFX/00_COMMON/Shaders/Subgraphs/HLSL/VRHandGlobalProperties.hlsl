#ifndef VR_HAND_POSITIONS
#define VR_HAND_POSITIONS

float3 _RightHandPos;
float3 _LeftHandPos;

void PlayerHandsPositions_float(out float3 LeftHandPosition, out float3 RightHandPosition)
{
    #ifdef SHADERGRAPH_PREVIEW
    RightHandPosition = float3(0,0,0);
    LeftHandPosition = float3(0,0,0);
    #else
    RightHandPosition = _RightHandPos;
    LeftHandPosition = _LeftHandPos;
    #endif
}

void PlayerHandsPositions_half(out float3 LeftHandPosition, out float3 RightHandPosition)
{
    #ifdef SHADERGRAPH_PREVIEW
    RightHandPosition = half3(0,0,0);
    LeftHandPosition = half3(0,0,0);
    #else
    RightHandPosition = _RightHandPos;
    LeftHandPosition = _LeftHandPos;
    #endif
}

#endif
