#ifndef SQUARE_TOWER_CHARACTER_CORNER_MASK
#define SQUARE_TOWER_CHARACTER_CORNER_MASK
// Pixel coordinates within the original sprite, with a top-left origin.
float CharacterCornerAlpha(float2 pixel, float4 headRect, float radius)
{
    radius = min(max(radius,0),min(headRect.z,headRect.w)*0.5);
    float2 distance = abs(pixel-headRect.xy-headRect.zw*0.5)-(headRect.zw*0.5-radius);
    float edge = length(max(distance,0))+min(max(distance.x,distance.y),0)-radius;
    return saturate(0.5-edge);
}
#endif
