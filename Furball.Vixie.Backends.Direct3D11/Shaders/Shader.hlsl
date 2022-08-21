struct VS_Input
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD;
    float4 Color    : COLOR;
    nointerpolation int InstanceTextureId2 : TEXID2;
    nointerpolation int InstanceTextureId1 : TEXID;
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

VS_Output VS_Main(const VS_Input input)
{
    VS_Output output;

    output.Position = mul(ProjectionMatrix, float4(input.Position.x, input.Position.y, 0, 1));
    output.Color = input.Color;
    output.TexCoord = input.TexCoord;
    output.TextureId = input.InstanceTextureId1 + input.InstanceTextureId2;

    return output;
}

Texture2D    Textures[128] : register(t0);
SamplerState Sampler : register(s0);

float4 PS_Main(const VS_Output input) : SV_Target
{
    switch(input.TextureId)
    {
        case 0:   return Textures[0].Sample(Sampler, input.TexCoord) * input.Color;
        case 1:   return Textures[1].Sample(Sampler, input.TexCoord) * input.Color;
        case 2:   return Textures[2].Sample(Sampler, input.TexCoord) * input.Color;
        case 3:   return Textures[3].Sample(Sampler, input.TexCoord) * input.Color;
        case 4:   return Textures[4].Sample(Sampler, input.TexCoord) * input.Color;
        case 5:   return Textures[5].Sample(Sampler, input.TexCoord) * input.Color;
        case 6:   return Textures[6].Sample(Sampler, input.TexCoord) * input.Color;
        case 7:   return Textures[7].Sample(Sampler, input.TexCoord) * input.Color;
        case 8:   return Textures[8].Sample(Sampler, input.TexCoord) * input.Color;
        case 9:   return Textures[9].Sample(Sampler, input.TexCoord) * input.Color;
        case 10:  return Textures[10].Sample(Sampler, input.TexCoord) * input.Color;
        case 11:  return Textures[11].Sample(Sampler, input.TexCoord) * input.Color;
        case 12:  return Textures[12].Sample(Sampler, input.TexCoord) * input.Color;
        case 13:  return Textures[13].Sample(Sampler, input.TexCoord) * input.Color;
        case 14:  return Textures[14].Sample(Sampler, input.TexCoord) * input.Color;
        case 15:  return Textures[15].Sample(Sampler, input.TexCoord) * input.Color;
        case 16:  return Textures[16].Sample(Sampler, input.TexCoord) * input.Color;
        case 17:  return Textures[17].Sample(Sampler, input.TexCoord) * input.Color;
        case 18:  return Textures[18].Sample(Sampler, input.TexCoord) * input.Color;
        case 19:  return Textures[19].Sample(Sampler, input.TexCoord) * input.Color;
        case 20:  return Textures[20].Sample(Sampler, input.TexCoord) * input.Color;
        case 21:  return Textures[21].Sample(Sampler, input.TexCoord) * input.Color;
        case 22:  return Textures[22].Sample(Sampler, input.TexCoord) * input.Color;
        case 23:  return Textures[23].Sample(Sampler, input.TexCoord) * input.Color;
        case 24:  return Textures[24].Sample(Sampler, input.TexCoord) * input.Color;
        case 25:  return Textures[25].Sample(Sampler, input.TexCoord) * input.Color;
        case 26:  return Textures[26].Sample(Sampler, input.TexCoord) * input.Color;
        case 27:  return Textures[27].Sample(Sampler, input.TexCoord) * input.Color;
        case 28:  return Textures[28].Sample(Sampler, input.TexCoord) * input.Color;
        case 29:  return Textures[29].Sample(Sampler, input.TexCoord) * input.Color;
        case 30:  return Textures[30].Sample(Sampler, input.TexCoord) * input.Color;
        case 31:  return Textures[31].Sample(Sampler, input.TexCoord) * input.Color;
        case 32:  return Textures[32].Sample(Sampler, input.TexCoord) * input.Color;
        case 33:  return Textures[33].Sample(Sampler, input.TexCoord) * input.Color;
        case 34:  return Textures[34].Sample(Sampler, input.TexCoord) * input.Color;
        case 35:  return Textures[35].Sample(Sampler, input.TexCoord) * input.Color;
        case 36:  return Textures[36].Sample(Sampler, input.TexCoord) * input.Color;
        case 37:  return Textures[37].Sample(Sampler, input.TexCoord) * input.Color;
        case 38:  return Textures[38].Sample(Sampler, input.TexCoord) * input.Color;
        case 39:  return Textures[39].Sample(Sampler, input.TexCoord) * input.Color;
        case 40:  return Textures[40].Sample(Sampler, input.TexCoord) * input.Color;
        case 41:  return Textures[41].Sample(Sampler, input.TexCoord) * input.Color;
        case 42:  return Textures[42].Sample(Sampler, input.TexCoord) * input.Color;
        case 43:  return Textures[43].Sample(Sampler, input.TexCoord) * input.Color;
        case 44:  return Textures[44].Sample(Sampler, input.TexCoord) * input.Color;
        case 45:  return Textures[45].Sample(Sampler, input.TexCoord) * input.Color;
        case 46:  return Textures[46].Sample(Sampler, input.TexCoord) * input.Color;
        case 47:  return Textures[47].Sample(Sampler, input.TexCoord) * input.Color;
        case 48:  return Textures[48].Sample(Sampler, input.TexCoord) * input.Color;
        case 49:  return Textures[49].Sample(Sampler, input.TexCoord) * input.Color;
        case 50:  return Textures[50].Sample(Sampler, input.TexCoord) * input.Color;
        case 51:  return Textures[51].Sample(Sampler, input.TexCoord) * input.Color;
        case 52:  return Textures[52].Sample(Sampler, input.TexCoord) * input.Color;
        case 53:  return Textures[53].Sample(Sampler, input.TexCoord) * input.Color;
        case 54:  return Textures[54].Sample(Sampler, input.TexCoord) * input.Color;
        case 55:  return Textures[55].Sample(Sampler, input.TexCoord) * input.Color;
        case 56:  return Textures[56].Sample(Sampler, input.TexCoord) * input.Color;
        case 57:  return Textures[57].Sample(Sampler, input.TexCoord) * input.Color;
        case 58:  return Textures[58].Sample(Sampler, input.TexCoord) * input.Color;
        case 59:  return Textures[59].Sample(Sampler, input.TexCoord) * input.Color;
        case 60:  return Textures[60].Sample(Sampler, input.TexCoord) * input.Color;
        case 61:  return Textures[61].Sample(Sampler, input.TexCoord) * input.Color;
        case 62:  return Textures[62].Sample(Sampler, input.TexCoord) * input.Color;
        case 63:  return Textures[63].Sample(Sampler, input.TexCoord) * input.Color;
        case 64:  return Textures[64].Sample(Sampler, input.TexCoord) * input.Color;
        case 65:  return Textures[65].Sample(Sampler, input.TexCoord) * input.Color;
        case 66:  return Textures[66].Sample(Sampler, input.TexCoord) * input.Color;
        case 67:  return Textures[67].Sample(Sampler, input.TexCoord) * input.Color;
        case 68:  return Textures[68].Sample(Sampler, input.TexCoord) * input.Color;
        case 69:  return Textures[69].Sample(Sampler, input.TexCoord) * input.Color;
        case 70:  return Textures[70].Sample(Sampler, input.TexCoord) * input.Color;
        case 71:  return Textures[71].Sample(Sampler, input.TexCoord) * input.Color;
        case 72:  return Textures[72].Sample(Sampler, input.TexCoord) * input.Color;
        case 73:  return Textures[73].Sample(Sampler, input.TexCoord) * input.Color;
        case 74:  return Textures[74].Sample(Sampler, input.TexCoord) * input.Color;
        case 75:  return Textures[75].Sample(Sampler, input.TexCoord) * input.Color;
        case 76:  return Textures[76].Sample(Sampler, input.TexCoord) * input.Color;
        case 77:  return Textures[77].Sample(Sampler, input.TexCoord) * input.Color;
        case 78:  return Textures[78].Sample(Sampler, input.TexCoord) * input.Color;
        case 79:  return Textures[79].Sample(Sampler, input.TexCoord) * input.Color;
        case 80:  return Textures[80].Sample(Sampler, input.TexCoord) * input.Color;
        case 81:  return Textures[81].Sample(Sampler, input.TexCoord) * input.Color;
        case 82:  return Textures[82].Sample(Sampler, input.TexCoord) * input.Color;
        case 83:  return Textures[83].Sample(Sampler, input.TexCoord) * input.Color;
        case 84:  return Textures[84].Sample(Sampler, input.TexCoord) * input.Color;
        case 85:  return Textures[85].Sample(Sampler, input.TexCoord) * input.Color;
        case 86:  return Textures[86].Sample(Sampler, input.TexCoord) * input.Color;
        case 87:  return Textures[87].Sample(Sampler, input.TexCoord) * input.Color;
        case 88:  return Textures[88].Sample(Sampler, input.TexCoord) * input.Color;
        case 89:  return Textures[89].Sample(Sampler, input.TexCoord) * input.Color;
        case 90:  return Textures[90].Sample(Sampler, input.TexCoord) * input.Color;
        case 91:  return Textures[91].Sample(Sampler, input.TexCoord) * input.Color;
        case 92:  return Textures[92].Sample(Sampler, input.TexCoord) * input.Color;
        case 93:  return Textures[93].Sample(Sampler, input.TexCoord) * input.Color;
        case 94:  return Textures[94].Sample(Sampler, input.TexCoord) * input.Color;
        case 95:  return Textures[95].Sample(Sampler, input.TexCoord) * input.Color;
        case 96:  return Textures[96].Sample(Sampler, input.TexCoord) * input.Color;
        case 97:  return Textures[97].Sample(Sampler, input.TexCoord) * input.Color;
        case 98:  return Textures[98].Sample(Sampler, input.TexCoord) * input.Color;
        case 99:  return Textures[99].Sample(Sampler, input.TexCoord) * input.Color;
        case 100: return Textures[100].Sample(Sampler, input.TexCoord) * input.Color;
        case 101: return Textures[101].Sample(Sampler, input.TexCoord) * input.Color;
        case 102: return Textures[102].Sample(Sampler, input.TexCoord) * input.Color;
        case 103: return Textures[103].Sample(Sampler, input.TexCoord) * input.Color;
        case 104: return Textures[104].Sample(Sampler, input.TexCoord) * input.Color;
        case 105: return Textures[105].Sample(Sampler, input.TexCoord) * input.Color;
        case 106: return Textures[106].Sample(Sampler, input.TexCoord) * input.Color;
        case 107: return Textures[107].Sample(Sampler, input.TexCoord) * input.Color;
        case 108: return Textures[108].Sample(Sampler, input.TexCoord) * input.Color;
        case 109: return Textures[109].Sample(Sampler, input.TexCoord) * input.Color;
        case 110: return Textures[110].Sample(Sampler, input.TexCoord) * input.Color;
        case 111: return Textures[111].Sample(Sampler, input.TexCoord) * input.Color;
        case 112: return Textures[112].Sample(Sampler, input.TexCoord) * input.Color;
        case 113: return Textures[113].Sample(Sampler, input.TexCoord) * input.Color;
        case 114: return Textures[114].Sample(Sampler, input.TexCoord) * input.Color;
        case 115: return Textures[115].Sample(Sampler, input.TexCoord) * input.Color;
        case 116: return Textures[116].Sample(Sampler, input.TexCoord) * input.Color;
        case 117: return Textures[117].Sample(Sampler, input.TexCoord) * input.Color;
        case 118: return Textures[118].Sample(Sampler, input.TexCoord) * input.Color;
        case 119: return Textures[119].Sample(Sampler, input.TexCoord) * input.Color;
        case 120: return Textures[120].Sample(Sampler, input.TexCoord) * input.Color;
        case 121: return Textures[121].Sample(Sampler, input.TexCoord) * input.Color;
        case 122: return Textures[122].Sample(Sampler, input.TexCoord) * input.Color;
        case 123: return Textures[123].Sample(Sampler, input.TexCoord) * input.Color;
        case 124: return Textures[124].Sample(Sampler, input.TexCoord) * input.Color;
        case 125: return Textures[125].Sample(Sampler, input.TexCoord) * input.Color;
        case 126: return Textures[126].Sample(Sampler, input.TexCoord) * input.Color;
        case 127: return Textures[127].Sample(Sampler, input.TexCoord) * input.Color;
        default:  return float4(1, 1, 1, 1);
    }
}