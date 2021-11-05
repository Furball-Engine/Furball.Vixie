#version 330 core

layout (location = 0) out vec4 Color;

in vec2 v_TexCoord;
in float v_TexIndex;
in vec4 v_Color;

uniform sampler2D u_Textures[32];

void main() {
    int index = int(v_TexIndex);
    //Thanks AMD
    switch(index) {
        case 0:
            Color = texture(u_Textures[0], v_TexCoord) * v_Color;
            break;
        case 1:
            Color = texture(u_Textures[1], v_TexCoord) * v_Color;
            break;
        case 2:
            Color = texture(u_Textures[2], v_TexCoord) * v_Color;
            break;
        case 3:
            Color = texture(u_Textures[3], v_TexCoord) * v_Color;
            break;
        case 4:
            Color = texture(u_Textures[4], v_TexCoord) * v_Color;
            break;
        case 5:
            Color = texture(u_Textures[5], v_TexCoord) * v_Color;
            break;
        case 6:
            Color = texture(u_Textures[6], v_TexCoord) * v_Color;
            break;
        case 7:
            Color = texture(u_Textures[7], v_TexCoord) * v_Color;
            break;
        case 8:
            Color = texture(u_Textures[8], v_TexCoord) * v_Color;
            break;
        case 9:
            Color = texture(u_Textures[9], v_TexCoord) * v_Color;
            break;
        case 10:
            Color = texture(u_Textures[10], v_TexCoord) * v_Color;
            break;
        case 11:
            Color = texture(u_Textures[11], v_TexCoord) * v_Color;
            break;
        case 12:
            Color = texture(u_Textures[12], v_TexCoord) * v_Color;
            break;
        case 13:
            Color = texture(u_Textures[13], v_TexCoord) * v_Color;
            break;
        case 14:
            Color = texture(u_Textures[14], v_TexCoord) * v_Color;
            break;
        case 15:
            Color = texture(u_Textures[15], v_TexCoord) * v_Color;
            break;
        case 16:
            Color = texture(u_Textures[16], v_TexCoord) * v_Color;
            break;
        case 17:
            Color = texture(u_Textures[17], v_TexCoord) * v_Color;
            break;
        case 18:
            Color = texture(u_Textures[18], v_TexCoord) * v_Color;
            break;
        case 19:
            Color = texture(u_Textures[19], v_TexCoord) * v_Color;
            break;
        case 20:
            Color = texture(u_Textures[20], v_TexCoord) * v_Color;
            break;
        case 21:
            Color = texture(u_Textures[21], v_TexCoord) * v_Color;
            break;
        case 22:
            Color = texture(u_Textures[22], v_TexCoord) * v_Color;
            break;
        case 23:
            Color = texture(u_Textures[23], v_TexCoord) * v_Color;
            break;
        case 24:
            Color = texture(u_Textures[24], v_TexCoord) * v_Color;
            break;
        case 25:
            Color = texture(u_Textures[25], v_TexCoord) * v_Color;
            break;
        case 26:
            Color = texture(u_Textures[26], v_TexCoord) * v_Color;
            break;
        case 27:
            Color = texture(u_Textures[27], v_TexCoord) * v_Color;
            break;
        case 28:
            Color = texture(u_Textures[28], v_TexCoord) * v_Color;
            break;
        case 29:
            Color = texture(u_Textures[29], v_TexCoord) * v_Color;
            break;
        case 30:
            Color = texture(u_Textures[30], v_TexCoord) * v_Color;
            break;
        case 31:
            Color = texture(u_Textures[31], v_TexCoord) * v_Color;
            break;
    }
}