#version 330 core

#extension GL_ARB_bindless_texture : require

flat in uvec2 texHandle;
in vec4 color;
in vec2 texCoord;

out vec4 FragColor;

void main() {
    FragColor = texture2D(sampler2D(texHandle), texCoord) * color;
}