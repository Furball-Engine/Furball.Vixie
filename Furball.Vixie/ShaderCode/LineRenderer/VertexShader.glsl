#version 300 es

layout(location = 0) in vec4 pos_width;
layout(location = 1) in vec4 col;

uniform mat4 u_mvp;

out vec4 v_col;
out float v_line_width;

void main()
{
    v_col = col;
    v_line_width = pos_width.w;
    gl_Position = u_mvp * vec4(pos_width.xyz, 1.0);
}