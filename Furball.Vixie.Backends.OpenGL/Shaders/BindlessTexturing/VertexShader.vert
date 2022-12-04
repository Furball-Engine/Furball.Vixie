#version 330 core

layout(location = 0) in vec2 VertexPosition;
layout(location = 1) in vec2 TextureCoordinate;
layout(location = 2) in vec4 VertexColor;
layout(location = 3) in uvec2 TextureHandle; //bindless texture handle

uniform mat4 ProjectionMatrix;

flat out uvec2 texHandle;
out vec4 color;
out vec2 texCoord;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

    color = VertexColor;
    texCoord = TextureCoordinate;
    texHandle = TextureHandle;
}