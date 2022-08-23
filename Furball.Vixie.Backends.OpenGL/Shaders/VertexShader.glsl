#version 110

attribute vec2 VertexPosition;
attribute vec2 TextureCoordinate;
attribute vec4 VertexColor;
attribute float  TextureId2;
attribute float  TextureId;

varying vec4  _Color;
varying vec2  _TextureCoordinate;
varying float _TextureId;

uniform mat4 ProjectionMatrix;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);

    _Color = VertexColor;
    _TextureCoordinate = TextureCoordinate;
    _TextureId = TextureId + TextureId2;
}
