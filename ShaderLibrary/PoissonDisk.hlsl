#ifndef ARCTOON_POISSON_DISK_INCLUDED
#define ARCTOON_POISSON_DISK_INCLUDED

#define POISSON_SAMPLE_COUNT 32

// Uncomment to enable runtime Vogel Disk sampling with per-pixel random rotation.
// This eliminates regular banding artifacts at the cost of slight noise.
// Comment out to fall back to precomputed Poisson Disk array.
#define POISSON_DISK_RUNTIME

#if defined(POISSON_DISK_RUNTIME)

// Golden Angle in radians (~2.3999632)
#define GOLDEN_ANGLE 2.3999632297286533

// Non-const array, will be filled at runtime by InitPoissonDisk
static float2 poissonDisk[POISSON_SAMPLE_COUNT];

// Call this before accessing poissonDisk[].
// screenPos: pixel screen-space coordinate (e.g. positionSTS.xy * atlasSize)
// Uses Unity's InterleavedGradientNoise from Random.hlsl for per-pixel randomization.
void InitPoissonDisk(float2 screenPos)
{
    float rotation = InterleavedGradientNoise(screenPos, 0) * 6.28318530718; // TWO_PI
    for (int i = 0; i < POISSON_SAMPLE_COUNT; i++)
    {
        float r = sqrt((float)i + 0.5) / sqrt((float)POISSON_SAMPLE_COUNT);
        float theta = (float)i * GOLDEN_ANGLE + rotation;
        poissonDisk[i] = float2(r * cos(theta), r * sin(theta));
    }
}

#else // Precomputed mode

static const float2 poissonDisk[POISSON_SAMPLE_COUNT] =
{
    float2(-0.94201624, -0.39906216),
    float2( 0.94558609, -0.76890725),
    float2(-0.09418410, -0.92938870),
    float2( 0.34495938,  0.29387760),
    float2(-0.91588581,  0.45771432),
    float2(-0.81544232, -0.87912464),
    float2(-0.38277543,  0.27676845),
    float2( 0.97484398,  0.75648379),
    float2( 0.44323325, -0.97511554),
    float2( 0.53742981, -0.47373420),
    float2(-0.26496911, -0.41893023),
    float2( 0.79197514,  0.19090188),
    float2(-0.24188840,  0.99706507),
    float2(-0.81409955,  0.91437590),
    float2( 0.19984126,  0.78641367),
    float2( 0.14383161, -0.14100790),
    float2(-0.50000000,  0.71934025),
    float2( 0.63942957,  0.57974182),
    float2(-0.67819881, -0.18047985),
    float2( 0.18301927, -0.69769883),
    float2( 0.37623652,  0.86347118),
    float2(-0.44275653, -0.70125830),
    float2( 0.81863004,  0.43468746),
    float2(-0.73284435,  0.66833752),
    float2( 0.07231909, -0.42786568),
    float2( 0.56201714,  0.09843390),
    float2(-0.15584070,  0.47843215),
    float2(-0.98757905,  0.13329709),
    float2( 0.31604116, -0.24610095),
    float2(-0.36972982,  0.95514798),
    float2( 0.86424630, -0.30479616),
    float2(-0.58070576, -0.48246527)
};

void InitPoissonDisk(float2 screenPos) {}

#endif // POISSON_DISK_RUNTIME

#endif // ARCTOON_POISSON_DISK_INCLUDED