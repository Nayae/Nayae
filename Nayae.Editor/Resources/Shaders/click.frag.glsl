#version 330 core

out vec4 FragColor;

uniform vec4 PickingColor;
uniform vec2 mouse;

void main()
{
    if (gl_FragCoord.x + 10 > mouse.x && gl_FragCoord.x - 10 < mouse.x && gl_FragCoord.y + 10 > mouse.y && gl_FragCoord.y - 10 < mouse.y){
        FragColor = vec4(PickingColor.xyz, 1.0f);
    } else {
        int index = gl_PrimitiveID + 244321;
        int r = (index >> 16) & 0xFF;
        int g = (index >> 8) & 0xFF;
        int b = index & 0xFF;

        FragColor = vec4(r / 255.0f, g / 255.0f, b / 255.0f, 1.0f);
    }
} 