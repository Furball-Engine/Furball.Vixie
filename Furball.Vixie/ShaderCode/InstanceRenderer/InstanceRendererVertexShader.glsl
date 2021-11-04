#version 330 core

layout(location = 0) in vec4 position;
layout(location = 1) in vec2 texCoord;

out vec2 v_TexCoord;
out vec4 v_Color;

uniform mat4 vx_WindowProjectionMatrix;
uniform mat4 u_RotationMatrix;

vec2 rotate(vec2 v, float a) {
	float s = sin(a);
	float c = cos(a);
	mat2 m = mat2(c, -s, s, c);
	return m * v;
}

void main() {
    //gl_Position = vx_WindowProjectionMatrix * u_Translation * position;
    //vec4 pos = u_RotationMatrix * vx_WindowProjectionMatrix * position;
    vec2 kurwa = rotate(vec2(position.x, position.y), 2);
    vec2 kurwa2 = rotate(texCoord, 2);
    //gl_Position = vx_WindowProjectionMatrix * position;
    gl_Position = vx_WindowProjectionMatrix * vec4(kurwa.x, kurwa.y, 0, 0);

    v_TexCoord = kurwa2;
}