struct VS_Input
{
    float2 Position       : POSITION;
    float2 TexCoord       : TEXCOORD;
    float2 Scale          : SCALE;
    float  Rotation       : ROTATION;
    float4 Color          : COLOR;
    float2 RotationOrigin : ROTORIGIN;
};

struct VS_Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
};

struct PS_Output
{
    float4 ColorOutput : SV_Target0;
};

cbuffer VS_ConstantBuffer : register(b0) {
    float4x4 ProjectionMatrix;
}

VS_Output VS_Main(VS_Input input)
{
    VS_Output output;

    float2 inputPos = mul(ProjectionMatrix, float4(input.Position.x, input.Position.y, 0, 1));

    float2x2 rotationMatrix = float2x2(cos(input.Rotation), sin(input.Rotation), -sin(input.Rotation), cos(input.Rotation));
    float2 newPosition = mul(rotationMatrix, inputPos);

    output.Position = float4(newPosition.x, newPosition.y, 0, 1);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}

Texture2D    Tex1 : register(t0);
SamplerState Sampler : register(s0);

PS_Output PS_Main(VS_Output input)
{
    PS_Output output;

    output.ColorOutput = Tex1.Sample(Sampler, input.TexCoord) * input.Color;

    return output;
}