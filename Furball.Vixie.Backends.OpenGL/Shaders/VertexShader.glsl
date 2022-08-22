#version 110

in vec2 VertexPosition;
in vec2 TextureCoordinate;
in vec4 VertexColor;
in int  TextureId2;
in int  TextureId;

varying vec4  _Color;
varying vec2  _TextureCoordinate;
varying float _TextureId;

uniform mat4 ProjectionMatrix;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

    _Color = VertexColor;
    _TextureCoordinate = TextureCoordinate;
    _TextureId = float(TextureId + TextureId2);
}
