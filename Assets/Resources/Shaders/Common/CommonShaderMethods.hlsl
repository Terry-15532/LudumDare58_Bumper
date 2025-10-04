//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float2 unity_gradientNoise_dir(float2 p){
    p = p % 289;
    float x = (34 * p.x + 1) * p.x % 289 + p.y;
    x = (34 * x + 1) * x % 289;
    x = frac(x / 41) * 2 - 1;
    return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
}

float unity_gradientNoise(float2 p){
    float2 ip = floor(p);
    float2 fp = frac(p);
    float d00 = dot(unity_gradientNoise_dir(ip), fp);
    float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
    float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
    float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
    fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
    return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
}

float GradientNoise(float2 UV, float Scale){
    return unity_gradientNoise(UV * Scale) + 0.5;
}

inline float unity_noise_randomValue(float2 uv){
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}

inline float unity_noise_interpolate(float a, float b, float t){
    return (1.0 - t) * a + (t * b);
}

inline float unity_valueNoise(float2 uv){
    float2 i = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    uv = abs(frac(uv) - 0.5);
    float2 c0 = i + float2(0.0, 0.0);
    float2 c1 = i + float2(1.0, 0.0);
    float2 c2 = i + float2(0.0, 1.0);
    float2 c3 = i + float2(1.0, 1.0);
    float r0 = unity_noise_randomValue(c0);
    float r1 = unity_noise_randomValue(c1);
    float r2 = unity_noise_randomValue(c2);
    float r3 = unity_noise_randomValue(c3);
    float bottomOfGrid = unity_noise_interpolate(r0, r1, f.x);
    float topOfGrid = unity_noise_interpolate(r2, r3, f.x);
    float t = unity_noise_interpolate(bottomOfGrid, topOfGrid, f.y);
    return t;
}

float3 ConvertNormal(float3 n, float3 vertTangent, float3 vertNormal){
    float3 bitangent = cross(vertNormal, vertTangent);
    float3x3 tbn = (float3x3(normalize(vertTangent), normalize(bitangent), normalize(vertNormal)));

    return normalize(mul(n, tbn));
}

/*float3 ConvertNormal3(float3 n, float3 vertTangent, float3 vertNormal){
    float3 bitangent = cross(vertNormal, vertTangent);
    float3x3 tbn = Inverse(float4x4(vertTangent,0, bitangent,0, vertNormal, 0));
    return normalize(mul(n, tbn));
}*/

float2 DecodeFloats(float combined){
    float b = combined - floor(combined);
    float a = floor(combined) / 10000;
    return float2(a, b);
}

float4 DecodeToFloat4(float2 encoded){
    return float4(DecodeFloats(encoded.x), DecodeFloats(encoded.y));
}

float3 TangentToObjectNormal(float3 tangentNormal, float3 normal, float3 tangent){
    float3 bitangent = cross(tangent, normal);

    float3x3 tbn = float3x3(tangent, bitangent, normal);

    float3 worldNormal = mul(tangentNormal, tbn);

    return normalize(worldNormal);
}

float3 GetAmbientColor(){
    return float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

float Brightness(float3 color){
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}


float3 ProjectionOnPlane(float3 input, float3 planeNormal){
    planeNormal = normalize(planeNormal);
    float dotProduct = dot(input, planeNormal);
    return input - dotProduct * planeNormal;
}

float Posterize(float t, float steps, float hardness = 1){
    float low = floor(t * steps) / steps;
    float high = low + 1 / steps;
    float diff = high - low;
    float mid = low + diff / 2;
    return low + diff * smoothstep(mid - diff * (1 - hardness) / 2, mid + diff * (1 - hardness) / 2, t);
}

float3 Overlay(float3 A, float3 B, float opacity){
    return A * (1 - opacity) + B * opacity;
}

float4 Overlay(float4 A, float4 B, float opacity){
    return A * (1 - opacity) + B * opacity;
}

float SimpleNoise(float2 UV, float Scale){
    float t = 0.0;
    float freq = pow(2.0, float(0));
    float amp = pow(0.5, float(3 - 0));
    t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
    freq = pow(2.0, float(1));
    amp = pow(0.5, float(3 - 1));
    t += unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
    freq = pow(2.0, float(2));
    amp = pow(0.5, float(3 - 2));
    return t + unity_valueNoise(float2(UV.x * Scale / freq, UV.y * Scale / freq)) * amp;
}

void Twirl(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out){
    float2 delta = UV - Center;
    float angle = Strength * length(delta);
    float x = cos(angle) * delta.x - sin(angle) * delta.y;
    float y = sin(angle) * delta.x + cos(angle) * delta.y;
    Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
}

float3 RotateAlongAxis(float3 In, float3 Axis, float Rotation){
    float s = sin(Rotation);
    float c = cos(Rotation);
    float one_minus_c = 1.0 - c;

    Axis = normalize(Axis);
    float3x3 rot_mat =
    {
        one_minus_c * Axis.x * Axis.x + c, one_minus_c * Axis.x * Axis.y - Axis.z * s, one_minus_c * Axis.z * Axis.x + Axis.y * s,
        one_minus_c * Axis.x * Axis.y + Axis.z * s, one_minus_c * Axis.y * Axis.y + c, one_minus_c * Axis.y * Axis.z - Axis.x * s,
        one_minus_c * Axis.z * Axis.x - Axis.y * s, one_minus_c * Axis.y * Axis.z + Axis.x * s, one_minus_c * Axis.z * Axis.z + c
    };
    return mul(rot_mat, In);
}

float3 Rotate(float3 v, float3 rotationDegrees){
    float3 rotationRadians = radians(rotationDegrees);

    float3x3 rotationX = float3x3(
        1, 0, 0,
        0, cos(rotationRadians.x), -sin(rotationRadians.x),
        0, sin(rotationRadians.x), cos(rotationRadians.x)
    );

    float3x3 rotationY = float3x3(
        cos(rotationRadians.y), 0, sin(rotationRadians.y),
        0, 1, 0,
        -sin(rotationRadians.y), 0, cos(rotationRadians.y)
    );

    float3x3 rotationZ = float3x3(
        cos(rotationRadians.z), -sin(rotationRadians.z), 0,
        sin(rotationRadians.z), cos(rotationRadians.z), 0,
        0, 0, 1
    );

    float3x3 rotationMatrix = mul(rotationZ, mul(rotationY, rotationX));

    return mul(rotationMatrix, v);
}
