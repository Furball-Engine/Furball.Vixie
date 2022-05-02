#version 410
#extension GL_ARB_bindless_texture : enable
#extension GL_NV_bindless_texture : enable

//Vertex data
layout (location = 0) in vec2 VertexPosition;
layout (location = 1) in vec2 VertexTextureCoordinate;

//Instance data
layout (location = 2) in vec2      InstancePos;
layout (location = 3) in vec2      InstanceSize;
layout (location = 4) in vec4      InstanceColor;
layout (location = 5) in vec2      InstanceTexturePosition;
layout (location = 6) in vec2      InstanceTextureSize;
layout (location = 7) in vec2      InstanceRotationOrigin;
layout (location = 8) in float     InstanceRotation;
layout (location = 9) in sampler2D InstanceTextureHandle;

     out vec4      fs_in_col;
     out vec2      fs_in_tex;
flat out sampler2D fs_in_texhandle;

uniform mat4  vx_WindowProjectionMatrix;

void main() {
    mat2 rotation_matrix = mat2(cos(InstanceRotation), sin(InstanceRotation),
                               -sin(InstanceRotation), cos(InstanceRotation));

    vec2 _VertexPosition = (VertexPosition * InstanceSize) - InstanceRotationOrigin;
    
    //Scale up the vertex to the specified size
    _VertexPosition = vec2(mat4(rotation_matrix) * vec4(_VertexPosition, 0, 0));
    //Move the vertex by the offset of the instance
    _VertexPosition = _VertexPosition + InstancePos;

    //Apply our projection matrix
    gl_Position = vx_WindowProjectionMatrix * vec4(_VertexPosition, 0, 1);
    
    fs_in_col = InstanceColor;
    fs_in_tex = (VertexTextureCoordinate * InstanceTextureSize) + InstanceTexturePosition;
    fs_in_tex.y *= -1.0f;
    fs_in_texhandle = InstanceTextureHandle;
}