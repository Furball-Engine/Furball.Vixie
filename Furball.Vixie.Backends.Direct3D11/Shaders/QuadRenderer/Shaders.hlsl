struct VS_Input
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD;

    float2              InstancePosition            : INSTANCE_POSITION;
    float2              InstanceSize                : INSTANCE_SIZE;
    float4              InstanceColor               : INSTANCE_COLOR;
    float2              InstanceTextureRectPosition : INSTANCE_TEXRECTPOSITION;
    float2              InstanceTextureRectSize     : INSTANCE_TEXRECTSIZE;
    float2              InstanceRotationOrigin      : INSTANCE_ROTORIGIN;
    float               InstanceRotation            : INSTANCE_ROTATION;
    nointerpolation int InstanceTextureId           : INSTANCE_TEXID;
};

struct VS_Output
{
    float4 Position  : SV_Position;
    float2 TexCoord  : TEXCOORD;
    float4 Color     : COLOR;
    int    TextureId : TEXID;
};

cbuffer VS_ConstantBuffer : register(b0) {
    float4x4 ProjectionMatrix;
}

VS_Output VS_Main(VS_Input input)
{
    VS_Output output;

    float c = cos(input.InstanceRotation);
    float s = sin(input.InstanceRotation);

    float x = input.InstanceRotationOrigin.x * (1 - c) + input.InstanceRotationOrigin.y * s;
    float y = input.InstanceRotationOrigin.y * (1 - c) - input.InstanceRotationOrigin.x * s;

    float4x4 rotMatrix = float4x4(c, s, 0, 0,
                                 -s, c, 0, 0,
                                  0, 0, 1, 0,
                                  x, y, 0, 1);

    float2 vertexPosition = (input.Position * input.InstanceSize) - input.InstanceRotationOrigin;
    vertexPosition = mul(rotMatrix, float4(vertexPosition.x, vertexPosition.y, 0, 1)).xy;

    vertexPosition = vertexPosition + input.InstancePosition;

    output.Position = mul(ProjectionMatrix, float4(vertexPosition.x, vertexPosition.y, 0, 1));
    output.Color = input.InstanceColor;
    output.TexCoord = (input.TexCoord * input.InstanceTextureRectSize) + input.InstanceTextureRectPosition;
    output.TextureId = input.InstanceTextureId;

    return output;
}

Texture2D    Textures[128] : register(t0);
SamplerState Sampler : register(s0);

float4 PS_Main(VS_Output input) : SV_Target
{
    switch(input.TextureId)
    {
        case 0: return Textures[0].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 1: return Textures[1].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 2: return Textures[2].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 3: return Textures[3].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 4: return Textures[4].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 5: return Textures[5].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 6: return Textures[6].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 7: return Textures[7].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 8: return Textures[8].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 9: return Textures[9].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 10: return Textures[10].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 11: return Textures[11].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 12: return Textures[12].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 13: return Textures[13].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 14: return Textures[14].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 15: return Textures[15].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 16: return Textures[16].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 17: return Textures[17].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 18: return Textures[18].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 19: return Textures[19].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 20: return Textures[20].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 21: return Textures[21].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 22: return Textures[22].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 23: return Textures[23].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 24: return Textures[24].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 25: return Textures[25].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 26: return Textures[26].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 27: return Textures[27].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 28: return Textures[28].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 29: return Textures[29].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 30: return Textures[30].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 31: return Textures[31].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 32: return Textures[32].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 33: return Textures[33].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 34: return Textures[34].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 35: return Textures[35].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 36: return Textures[36].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 37: return Textures[37].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 38: return Textures[38].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 39: return Textures[39].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 40: return Textures[40].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 41: return Textures[41].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 42: return Textures[42].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 43: return Textures[43].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 44: return Textures[44].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 45: return Textures[45].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 46: return Textures[46].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 47: return Textures[47].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 48: return Textures[48].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 49: return Textures[49].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 50: return Textures[50].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 51: return Textures[51].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 52: return Textures[52].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 53: return Textures[53].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 54: return Textures[54].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 55: return Textures[55].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 56: return Textures[56].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 57: return Textures[57].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 58: return Textures[58].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 59: return Textures[59].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 60: return Textures[60].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 61: return Textures[61].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 62: return Textures[62].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 63: return Textures[63].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 64: return Textures[64].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 65: return Textures[65].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 66: return Textures[66].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 67: return Textures[67].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 68: return Textures[68].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 69: return Textures[69].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 70: return Textures[70].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 71: return Textures[71].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 72: return Textures[72].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 73: return Textures[73].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 74: return Textures[74].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 75: return Textures[75].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 76: return Textures[76].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 77: return Textures[77].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 78: return Textures[78].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 79: return Textures[79].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 80: return Textures[80].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 81: return Textures[81].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 82: return Textures[82].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 83: return Textures[83].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 84: return Textures[84].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 85: return Textures[85].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 86: return Textures[86].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 87: return Textures[87].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 88: return Textures[88].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 89: return Textures[89].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 90: return Textures[90].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 91: return Textures[91].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 92: return Textures[92].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 93: return Textures[93].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 94: return Textures[94].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 95: return Textures[95].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 96: return Textures[96].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 97: return Textures[97].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 98: return Textures[98].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 99: return Textures[99].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 100: return Textures[100].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 101: return Textures[101].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 102: return Textures[102].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 103: return Textures[103].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 104: return Textures[104].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 105: return Textures[105].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 106: return Textures[106].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 107: return Textures[107].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 108: return Textures[108].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 109: return Textures[109].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 110: return Textures[110].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 111: return Textures[111].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 112: return Textures[112].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 113: return Textures[113].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 114: return Textures[114].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 115: return Textures[115].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 116: return Textures[116].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 117: return Textures[117].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 118: return Textures[118].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 119: return Textures[119].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 120: return Textures[120].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 121: return Textures[121].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 122: return Textures[122].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 123: return Textures[123].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 124: return Textures[124].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 125: return Textures[125].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 126: return Textures[126].Sample(Sampler, input.TexCoord) * input.Color; break;
        case 127: return Textures[127].Sample(Sampler, input.TexCoord) * input.Color; break;
    }

    return float4(1, 1, 1, 1);
}