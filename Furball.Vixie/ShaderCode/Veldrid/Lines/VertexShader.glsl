#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(location = 0) in vec4 pos_width;
layout(location = 1) in vec4 col;

layout(set = 0, binding = 0) uniform ProjectionMatrixUniform {
    mat4 ProjectionMatrix;
};

layout(location = 0) out vec4 v_col;
layout(location = 1) out float v_line_width;

void main()
{
    v_col = col;
    v_line_width = pos_width.w;
    gl_Position = ProjectionMatrix * vec4(pos_width.xyz, 1.0);
}