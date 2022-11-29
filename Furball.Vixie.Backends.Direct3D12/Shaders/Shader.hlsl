#define textureSpace space1
#define samplerSpace space1

struct VS_Input
{
                    float2  Position : POSITION;
                    float2  TexCoord : TEXCOORD;
                    float4  Color    : COLOR;
    nointerpolation uint    InstanceTextureId2 : TEXID;
    nointerpolation uint    InstanceTextureId1 : TEXID1;
};

struct VS_Output
{
    float4               Position  : SV_Position;
    float2               TexCoord  : TEXCOORD;
    float4               Color     : COLOR;
    nointerpolation uint TextureId : TEXID;
};

cbuffer VS_ConstantBuffer : register(b0) {
    float4x4 ProjectionMatrix;
}

VS_Output VS_Main(const VS_Input input)
{
    VS_Output output;

    output.Position = mul(ProjectionMatrix, float4(input.Position.x, input.Position.y, 0, 1));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.TextureId = input.InstanceTextureId1 + input.InstanceTextureId2;

    return output;
}

Texture2D    Textures[] : register(t0, textureSpace);
SamplerState Samplers[] : register(s0, samplerSpace);

float4 PS_Main(const VS_Output input) : SV_Target
{
    return Textures[input.TextureId].Sample(Samplers[input.TextureId], input.TexCoord) * input.Color;
}