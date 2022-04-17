cbuffer VertexConstantBuffer : register(b0)
{
    float4x4 ProjectionMatrix;
}

struct VS_Input
{
    float2 Position         : POSITION;
    float2 InstancePosition : INSTANCE_POSITION;
    float2 InstanceSize     : INSTANCE_SIZE;
    float4 InstanceColor    : INSTANCE_COLOR;
    float  InstanceRotation : INSTANCE_ROTATION;
};

struct VS_Output
{
    float4 Position : SV_Position;
    float4 OutColor : COLOR;
};

VS_Output VS_Main(VS_Input input)
{
    VS_Output output;

    float2x2 rotationMatrix = float2x2( cos(input.InstanceRotation), sin(input.InstanceRotation),
                                       -sin(input.InstanceRotation), cos(input.InstanceRotation));

    float2 rotationOrigin = float2(0, input.InstanceSize.y / 2.0f);
    float2 vertexPosition = (input.Position * input.InstanceSize) - rotationOrigin;

    vertexPosition = mul(rotationMatrix, vertexPosition);
    vertexPosition = vertexPosition + input.Position;

    output.Position = mul(ProjectionMatrix, float4(vertexPosition.x, vertexPosition.y, 0, 1));
    output.OutColor = input.InstanceColor;
}

float4 PS_Main(VS_Output input) : SV_Target
{
    return input.OutColor;
}