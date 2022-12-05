#version 410 core

layout(location = 0) in vec2 VertexPosition;
layout(location = 1) in vec2 TextureCoordinate;
layout(location = 2) in vec4 VertexColor;
layout(location = 3) in uint TextureHandle; //bindless texture handle

uniform mat4 ProjectionMatrix;

layout(location = 0) flat out uint texIndex;
layout(location = 1) out vec4 color;
layout(location = 2) out vec2 texCoord;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

    color = VertexColor;
    texCoord = TextureCoordinate;
    texIndex = TextureHandle;
}