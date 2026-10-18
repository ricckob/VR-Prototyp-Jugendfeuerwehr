#ifndef BURN_POSITION_MASK_INCLUDED
#define BURN_POSITION_MASK_INCLUDED

float4 _BurnPositions[64];
int _BurnPositionCount;

void BurnPositionMask_float(float3 WorldPos, float BurnRadius, out float Mask)
{
    float minDist = 99999.0;
    for (int i = 0; i < _BurnPositionCount; i++)
    {
        float d = distance(WorldPos, _BurnPositions[i].xyz);
        minDist = min(minDist, d);
    }
    Mask = saturate(1.0 - (minDist / max(BurnRadius, 0.001)));
}

#endif