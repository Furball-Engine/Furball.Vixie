#version 450

layout(location = 0) in vec4 _Color;
layout(location = 1) in vec2 _TextureCoordinate;
layout(location = 2) flat in int _TextureId;

layout(set = 1, binding = 0) uniform texture2D tex_0;
layout(set = 1, binding = 1) uniform sampler   sampler_0;
layout(set = 2, binding = 0) uniform texture2D tex_1;
layout(set = 2, binding = 1) uniform sampler   sampler_1;
layout(set = 3, binding = 0) uniform texture2D tex_2;
layout(set = 3, binding = 1) uniform sampler   sampler_2;
layout(set = 4, binding = 0) uniform texture2D tex_3;
layout(set = 4, binding = 1) uniform sampler   sampler_3;

layout(location = 0) out vec4 fsout_Color;

void main() {
    switch(_TextureId) {
        case 0:
            fsout_Color = texture(sampler2D(tex_0, sampler_0), _TextureCoordinate) * _Color;
            break;
        case 1:
            fsout_Color = texture(sampler2D(tex_1, sampler_1), _TextureCoordinate) * _Color;
            break;
        case 2:
            fsout_Color = texture(sampler2D(tex_2, sampler_2), _TextureCoordinate) * _Color;
            break;
        case 3:
            fsout_Color = texture(sampler2D(tex_3, sampler_3), _TextureCoordinate) * _Color;
            break;
    }
}
