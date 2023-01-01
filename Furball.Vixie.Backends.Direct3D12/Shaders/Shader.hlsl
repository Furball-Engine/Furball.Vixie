#define textureSpace space1
#define samplerSpace space1

#define RootSignatureDef "" \
"RootFlags( ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT ), " \
"RootConstants( b0, num32BitConstants = 16 ), " \
"DescriptorTable( " \
"    SRV(t0, numDescriptors = unbounded ), " \
"    visibility = SHADER_VISIBILITY_PIXEL" \
")," \
"DescriptorTable( " \
"    Sampler(s0, numDescriptors = unbounded), " \
"    visibility = SHADER_VISIBILITY_PIXEL" \
")"

struct VS_Input
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
    nointerpolation uint SamplerId : TEXID1;
    nointerpolation uint TextureId : TEXID;
};

struct VS_Output
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD;
    float4 Color : COLOR;
    nointerpolation uint TextureId : TEXID;
    nointerpolation uint SamplerId : TEXID1;
};

cbuffer VS_ConstantBuffer : register(b0) {
    float4x4 ProjectionMatrix;
}

[RootSignature(RootSignatureDef)]
VS_Output VS_Main(const VS_Input input)
{
    VS_Output output;

    output.Position = mul(ProjectionMatrix, float4(input.Position.x, input.Position.y, 0, 1));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.TextureId = input.TextureId;
    output.SamplerId = input.SamplerId;

    return output;
}

Texture2D Textures[] : register(t0);
SamplerState Samplers[] : register(s0);

float4 PS_Main(const VS_Output input) : SV_Target
{
    return Textures[input.TextureId].Sample(Samplers[input.SamplerId], input.TexCoord) * input.Color;
}