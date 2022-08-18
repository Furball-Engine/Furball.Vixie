#version 140

in vec2 VertexPosition;
in vec2 TextureCoordinate;
in vec4 VertexColor;
in int  TextureId;

uniform mat4 ProjectionMatrix;

out      vec4 _Color;
out      vec2 _TextureCoordinate;
flat out int  _TextureId;

void main() {
    gl_Position = ProjectionMatrix * vec4(VertexPosition, 0, 1);
    
    _Color = VertexColor;
    _TextureCoordinate = TextureCoordinate;
    _TextureId = TextureId;
}
