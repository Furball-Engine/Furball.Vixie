// Adapted from ocornut's Direct3D11 ImGui implementation
// License: https://github.com/ocornut/imgui/blob/master/LICENSE.txt

cbuffer VertexConstantBuffer
{
    float4x4 ProjectionMatrix;
}

struct VS_Input
{
    float2 Position : POSITION;
    float4 Color    : COLOR;
    float2 TexCoord : TEXCOORD;
};

struct VS_Output
{
    float2 Position : SV_POSITION;
    float4 Color    : COLOR;
    float2 TexCoord : TEXCOORD;
};

VS_Output VS_Main(VS_Input input)
{
    VS_Output output;

    output.Position = mul(ProjectionMatrix, float4(input.Position.xy, 0.f, 1.f));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

sampler Sampler0;
Texture2D Texture0;

float4 PS_Main(VS_Output input) : SV_Target
{
    return input.Color * Texture0.Sample(Sampler0, input.TexCoord);
}