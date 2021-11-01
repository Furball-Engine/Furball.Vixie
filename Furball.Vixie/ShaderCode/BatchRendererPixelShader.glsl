#version 330 core

layout (location = 0) out vec4 Color;

in vec2 v_TexCoord;
in float v_TexIndex;

uniform sampler2D u_Textures[32];

void main() {
    int index = int(v_TexIndex);
    Color = texture(u_Textures[index], v_TexCoord);
}