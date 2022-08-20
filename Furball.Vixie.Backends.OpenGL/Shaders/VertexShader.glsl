#version 330

layout(location = 0) in vec2 VertexPosition;
layout(location = 1) in vec2 TextureCoordinate;
layout(location = 2) in vec4 VertexColor;
layout(location = 3) in int  TextureId2;
layout(location = 4) in int  TextureId;

out      vec4 _Color;
out      vec2 _TextureCoordinate;
flat out int  _TextureId;

uniform mat4 ProjectionMatrix;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

    _Color = VertexColor;
    _TextureCoordinate = TextureCoordinate;
    _TextureId = TextureId + TextureId2;
}
