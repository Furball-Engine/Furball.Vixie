#version 450

layout(location = 0)      in vec4 fs_in_col;
layout(location = 1)      in vec2 fs_in_tex;
layout(location = 2) flat in int  fs_in_texid;

layout(set = 1, binding = 0) uniform texture2D tex_0;
layout(set = 1, binding = 1) uniform texture2D sampler_0;
layout(set = 2, binding = 0) uniform texture2D tex_1;
layout(set = 2, binding = 1) uniform texture2D sampler_1;
layout(set = 3, binding = 0) uniform texture2D tex_2;
layout(set = 3, binding = 1) uniform texture2D sampler_2;
layout(set = 4, binding = 0) uniform texture2D tex_3;
layout(set = 4, binding = 1) uniform texture2D sampler_3;

layout(location = 0) out vec4 fsout_Color;

void main() {
    switch(fs_in_texid) {
        case 0:
        fsout_Color = texture(sampler2D(tex_0, sampler_0), fs_in_tex) * fs_in_col;
        break;
        case 1:
        fsout_Color = texture(sampler2D(tex_1, sampler_1), fs_in_tex) * fs_in_col;
        break;
        case 2:
        fsout_Color = texture(sampler2D(tex_2, sampler_2), fs_in_tex) * fs_in_col;
        break;
        case 3:
        fsout_Color = texture(sampler2D(tex_3, sampler_3), fs_in_tex) * fs_in_col;
        break;
    }
}
