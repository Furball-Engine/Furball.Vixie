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

    float c = cos(input.Rotation);
    float s = sin(input.Rotation);

    float x = input.RotationOrigin.x * (1 - c) + input.RotationOrigin.y * s;
    float y = input.RotationOrigin.y * (1 - c) - input.RotationOrigin.x * s;

    float4x4 rotMatrix = float4x4(c, s, 0, 0,
                                 -s, c, 0, 0,
                                  0, 0, 1, 0,
                                  x, y, 0, 1);


    //float4 newPosition = mul(input.Position, float4(input.Position.x, input.Position.y, 0, 1));
    float4 newPosition = mul(float4(input.Position.x, input.Position.y, 0, 1), rotMatrix);

    output.Position = mul(ProjectionMatrix, newPosition);
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