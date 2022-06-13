
float ProjectionDistance(float3 targetPosition, float3 lightPosition)
{
    targetPosition.z = max(targetPosition.z, 0);
    float3 relativePosition = targetPosition - lightPosition;
    const float o = sqrt((relativePosition.x * relativePosition.x) + (relativePosition.y * relativePosition.y));
    const float a = max(0.001, lightPosition.z - targetPosition.z);
    return (o / a);
}

