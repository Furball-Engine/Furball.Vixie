#version 110

const int c_BatchCount = 128;

//Vertex data
attribute vec2  a_VertexPosition;
attribute vec2  a_VertexTextureCoordinate;

attribute float a_QuadIndex;

uniform vec4  u_QuadColors[c_BatchCount];
uniform vec2  u_QuadPositions[c_BatchCount];
uniform vec2  u_QuadSizes[c_BatchCount];
uniform vec2  u_QuadRotationOrigins[c_BatchCount];
uniform float u_QuadRotations[c_BatchCount];
uniform float u_QuadTextureIds[c_BatchCount];
uniform vec4  u_QuadTextureCoordinates[c_BatchCount];

uniform mat4 u_ProjectionMatrix;

varying vec4  fs_in_col;
varying vec2  fs_in_tex;
varying float fs_in_texid;

void main() {
    int quadIndex = int(a_QuadIndex);
    
    float rotation = u_QuadRotations[quadIndex];
    
    mat2 rotation_matrix = mat2(cos(rotation), sin(rotation),
                               -sin(rotation), cos(rotation));
    
    vec2 quadPosition = u_QuadPositions[quadIndex];
    vec2 quadSize = u_QuadSizes[quadIndex];
    
    vec2 _VertexPosition = a_VertexPosition * quadSize - u_QuadRotationOrigins[quadIndex];
    
    //Rotate the point
    _VertexPosition = rotation_matrix * _VertexPosition;
    //Move the vertex by the offset of the instance
    _VertexPosition = _VertexPosition + quadPosition;

    //Apply our projection matrix
    gl_Position = u_ProjectionMatrix * vec4(_VertexPosition, 0, 1);
    
    fs_in_col = u_QuadColors[quadIndex];
    fs_in_tex = (a_VertexTextureCoordinate * u_QuadTextureCoordinates[quadIndex].zw) + u_QuadTextureCoordinates[quadIndex].xy;
    fs_in_tex.y *= -1.0;
    fs_in_texid = u_QuadTextureIds[quadIndex];
}