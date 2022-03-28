#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(set = 0, binding = 0) uniform ProjectionMatrixUniform
{
    mat4 ProjectionMatrix;
};

//The main vertices
layout(location = 0) in vec2 VertexPosition;

//The instance data
layout(location = 1) in vec2  InstancePosition;
layout(location = 2) in vec2  InstanceSize;
layout(location = 3) in vec4  InstanceColor;
layout(location = 4) in float InstanceRotation;

layout(location = 0) out vec4 fs_in_col;

void main() {
    mat2 rotation_matrix = mat2(cos(InstanceRotation), sin(InstanceRotation),
    -sin(InstanceRotation), cos(InstanceRotation));

    vec2 InstanceRotationOrigin = vec2(0, InstanceSize.y / 2.0f);
    
    vec2 _VertexPosition = (VertexPosition * InstanceSize) - InstanceRotationOrigin;

    //Scale up the vertex to the specified size
    _VertexPosition = vec2(mat4(rotation_matrix) * vec4(_VertexPosition, 0, 0));
    //Move the vertex by the offset of the instance
    _VertexPosition = _VertexPosition + InstancePosition;

    //Apply our projection matrix
    gl_Position = ProjectionMatrix * vec4(_VertexPosition, 0, 1);

    fs_in_col = InstanceColor;
}
