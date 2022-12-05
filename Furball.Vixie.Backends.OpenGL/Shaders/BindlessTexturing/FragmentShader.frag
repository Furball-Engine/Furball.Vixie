#version 410 core

#extension GL_ARB_bindless_texture : require

layout(location = 0) flat in uint texIndex;
layout(location = 1) in vec4 color;
layout(location = 2) in vec2 texCoord;

layout(location = 0) out vec4 FragColor;

struct TextureUniformEntry {
    sampler2D Texture;
    uint _pad0;
    uint _pad1;
};

layout (std140) uniform TextureUniform {
    TextureUniformEntry Textures[256];
};

void main() {
    FragColor = texture2D(Textures[texIndex].Texture, texCoord) * color;
}