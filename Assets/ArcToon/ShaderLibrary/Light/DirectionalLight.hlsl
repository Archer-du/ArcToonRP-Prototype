#ifndef ARCTOON_LIGHT_INCLUDED
#define ARCTOON_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 8

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct DirectionalLight {
	float3 color;
	float3 direction;
};

int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

DirectionalLight GetDirectionalLight(int index) {
	DirectionalLight light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	return light;
}
#endif