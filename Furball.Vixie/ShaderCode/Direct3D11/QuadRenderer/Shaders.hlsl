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

VS_Output VS_Main(VS_Input input)
{
    VS_Output output;

    output.Position = float4(input.Position.x, input.Position.y, 0, 1);
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;

    return output;
}


Texture2D    mytexture : register(t0);
SamplerState mysampler : register(s0);

PS_Output PS_Main(VS_Output input)
{
    PS_Output output;

    output.ColorOutput = mytexture.Sample(mysampler, input.TexCoord) * input.Color;

    return output;
}