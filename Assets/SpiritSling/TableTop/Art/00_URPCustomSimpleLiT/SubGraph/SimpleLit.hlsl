//FROM Cyanilux https://github.com/Cyanilux/URP_ShaderGraphCustomLighting
void GetMainLight_float(out float3 Direction, out float3 Color, out float DistanceAtten)
{
    #ifdef SHADERGRAPH_PREVIEW
		Direction = normalize(float3(1,1,-0.4));
		Color = float4(1,1,1,1);
		DistanceAtten = 1;
    #else
    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    #endif
}

void GetMainLight_half(out half3 Direction, out half3 Color, out half DistanceAtten)
{
    #ifdef SHADERGRAPH_PREVIEW
		Direction = normalize(float3(1,1,-0.4));
		Color = float4(1,1,1,1);
		DistanceAtten = 1;
    #else
    Light mainLight = GetMainLight();
    Direction = mainLight.direction;
    Color = mainLight.color;
    DistanceAtten = mainLight.distanceAttenuation;
    #endif
}

void Lambert_half(half3 lightColor, half3 lightDir, float3 normal, out half3 _color)
{
    half NdotL = saturate(dot(normal, lightDir));
    _color = lightColor * NdotL;
}

void Lambert_float(float3 lightColor, float3 lightDir, float3 normal, out float3 _color)
{
    float NdotL = saturate(dot(normal, lightDir));
    _color = lightColor * NdotL;
}


//FROM Cyanilux https://github.com/Cyanilux/URP_ShaderGraphCustomLighting
/*
- Samples the Shadowmap for the Main Light, based on the World Position passed in. (Position node)
- For shadows to work in the Unlit Graph, the following keywords must be defined in the blackboard :
	- Enum Keyword, Global Multi-Compile "_MAIN_LIGHT", with entries :
		- "SHADOWS"
		- "SHADOWS_CASCADE"
		- "SHADOWS_SCREEN"
	- Boolean Keyword, Global Multi-Compile "_SHADOWS_SOFT"
- For a PBR/Lit Graph, these keywords are already handled for you.
*/

void GetMainLightShadows_float(float3 WorldPos, half4 Shadowmask, out float ShadowAtten)
{
  //  #ifdef SHADERGRAPH_PREVIEW
		ShadowAtten = 1;
  //  #else
  //  #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
		//float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
  //  #else
  //  float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
  //  #endif
  //  ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
  //  #endif
}

void GetMainLightShadows_half(half3 WorldPos, half4 Shadowmask, out half ShadowAtten)
{
  //  #ifdef SHADERGRAPH_PREVIEW
		ShadowAtten = 1;
  //  #else
  //  #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
		//float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
  //  #else
  //  half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
  //  #endif
  //  ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
  //  #endif
}
