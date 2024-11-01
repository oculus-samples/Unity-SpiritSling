#ifndef TABLETOP_TILES_GLOBAL_PROPERTIES
#define TABLETOP_TILES_GLOBAL_PROPERTIES

float _ShakeIntensity;
float _ShakeFrequency;
float _VerticalShakeFrequency;

void GetTilesGlobalProperties_float(out float ShakeIntensity, out float ShakeFrequency,
                                    out float VerticalShakeFrequency)
{
    #ifdef SHADERGRAPH_PREVIEW
    ShakeIntensity = 0.1f;
    ShakeFrequency = 0.1f;
    VerticalShakeFrequency = 0.1f;
    #else
    ShakeIntensity = _ShakeIntensity;
    ShakeFrequency = _ShakeFrequency;
    VerticalShakeFrequency = _VerticalShakeFrequency;
    #endif
}

void GetTilesGlobalProperties_half(out half ShakeIntensity, out half ShakeFrequency, out half VerticalShakeFrequency)
{
    #ifdef SHADERGRAPH_PREVIEW
    ShakeIntensity = 0.1f;
    ShakeFrequency = 0.1f;
    VerticalShakeFrequency = 0.1f;
    #else
    ShakeIntensity = _ShakeIntensity;
    ShakeFrequency = _ShakeFrequency;
    VerticalShakeFrequency = _VerticalShakeFrequency;
    #endif
}


#endif
