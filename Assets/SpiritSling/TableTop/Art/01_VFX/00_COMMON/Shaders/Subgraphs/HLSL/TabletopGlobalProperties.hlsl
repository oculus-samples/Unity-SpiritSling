#ifndef TABLETOP_BOARD_GLOBAL_PROPERTIES
#define TABLETOP_BOARD_GLOBAL_PROPERTIES

float3 _TableWorldPosition;

void GetBoardGlobalProperties_float(out float3 TableWorldPosition)
{
    #ifdef SHADERGRAPH_PREVIEW
    TableWorldPosition = float3(0,0,0);
    #else
    TableWorldPosition = _TableWorldPosition;
    #endif
}

void GetBoardGlobalProperties_half(out half3 TableWorldPosition)
{
    #ifdef SHADERGRAPH_PREVIEW
    TableWorldPosition = float3(0,0,0);
    #else
    TableWorldPosition = _TableWorldPosition;
    #endif
}

float3 _LocalPlayerColor;

void GetLocalPlayerColor_float(out float3 PlayerColor)
{
    #ifdef SHADERGRAPH_PREVIEW
    PlayerColor = float3(1,0,0);
    #else
    PlayerColor = _LocalPlayerColor;
    #endif
}

void GetLocalPlayerColor_half(out half3 PlayerColor)
{
    #ifdef SHADERGRAPH_PREVIEW
    PlayerColor = float3(1,0,0);
    #else
    PlayerColor = _LocalPlayerColor;
    #endif
}

float _StoneInteractionRange;
float _StoneSkipRange;
float _AllowPhaseSkip;

void GetStoneInteractionProperties_float(out float StoneInteractionRange, out float StoneSkipRange,
                                         out float AllowPhaseSkip)
{
    #ifdef SHADERGRAPH_PREVIEW
    StoneInteractionRange = 0.2f;
    StoneSkipRange = 0.05f;
    AllowPhaseSkip = 1;
    #else
    StoneInteractionRange = _StoneInteractionRange;
    StoneSkipRange = _StoneSkipRange;
    AllowPhaseSkip = _AllowPhaseSkip;
    #endif
}

void GetStoneInteractionProperties_half(out half StoneInteractionRange, out half StoneSkipRange,
                                        out half AllowPhaseSkip)
{
    #ifdef SHADERGRAPH_PREVIEW
    StoneInteractionRange = 0.2f;
    StoneSkipRange = 0.05f;
    AllowPhaseSkip = 1;
    #else
    StoneInteractionRange = _StoneInteractionRange;
    StoneSkipRange = _StoneSkipRange;
    AllowPhaseSkip = _AllowPhaseSkip;
    #endif
}

#endif
