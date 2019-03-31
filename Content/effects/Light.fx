#define MAXLIGHT 20

matrix World;
matrix WorldViewProj;
float3 CameraPosition;
float3 SunLightDirection;
float4 SunLightColor;
float SunLightIntensity;

float3 PointLightPosition[MAXLIGHT];
float4 PointLightColor[MAXLIGHT];
float PointLightIntensity[MAXLIGHT];
float PointLightRadius[MAXLIGHT];
int MaxLightsRendered = 0;
Texture2D DiffuseTexture;
float3 Xposition;


SamplerState textureSampler
{
	MinFilter = linear;
	MagFilter = Anisotropic;
	AddressU = Wrap;
	AddressV = Wrap;
};

struct VertexShaderInput
{
	//float4 Position : SV_POSITION0;
#if SM4
	float4 Position : SV_POSITION0;
#else
	float4 Position : POSITION0;
#endif
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput
{
	//float4 Position : SV_POSITION0;
#if SM4
	float4 Position : SV_POSITION0;
#else
	float4 Position : POSITION0;
#endif
	float3 Normal : NORMAL0;
	float2 TexCoord : TEXCOORD0;
	float3 WorldPos : TEXCOORD1;
	float3 CamPos : POSITION1;
};


float4 CalcDiffuseLight(float3 normal, float3 lightDirection, float4 lightColor, float lightIntensity)
{
	return saturate(dot(normal, -lightDirection)) * lightIntensity * lightColor;
}

float4 CalcSpecularLight(float3 normal, float3 lightDirection, float3 cameraDirection, float4 lightColor, float lightIntensity)
{
	//float4 specular = SpecularIntensity * SpecularColor * max(pow(dotProduct, Shininess), 0) * length(input.Color);

	float3 halfVector = normalize(-lightDirection + -cameraDirection);
	float specular = saturate(dot(halfVector, normal));

	//I have all models be the same reflectance
	float specularPower = 20;

	return lightIntensity * lightColor * pow(abs(specular), specularPower);
}

float lengthSquared(float3 v1)
{
	return v1.x * v1.x + v1.y * v1.y + v1.z * v1.z;
}

float3 getCamPos()
{
	return CameraPosition;
}

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput Output;

	float3 cp = getCamPos();

	Output.CamPos = cp;
	//Calculate the position on screen
	Output.Position = mul(input.Position, WorldViewProj);

	//Transform the normal to world space
	Output.Normal = mul(float4(input.Normal, 0), World).xyz;

	//UV coordinates for our textures
	Output.TexCoord = input.TexCoord;

	//The position of our vertex in world space
	Output.WorldPos = mul(input.Position, World).xyz;

	return Output;
}

float4 PS(VertexShaderOutput input) : SV_TARGET
{
	float4 baseColor = DiffuseTexture.Sample(textureSampler, input.TexCoord);


	float4 diffuseLight = float4(0, 0, 0, 0);
	float4 specularLight = float4(0, 0, 0, 0);
	//float3 camPos = (float3)PositionOfCamera;

	//calculate our viewDirection
	float3 cameraDirection = normalize(input.WorldPos - input.CamPos);

	//calculate our sunlight
	diffuseLight += CalcDiffuseLight(input.Normal, SunLightDirection, SunLightColor, SunLightIntensity);
	specularLight += CalcSpecularLight(input.Normal, SunLightDirection, cameraDirection, SunLightColor, SunLightIntensity);

	//calculate our pointLights
	[loop]
	for (int i = 0; i < MaxLightsRendered; i++)
	{
		float3 PointLightDirection = input.WorldPos - PointLightPosition[i];

		float DistanceSq = lengthSquared(PointLightDirection);

		float radius = PointLightRadius[i];

		[branch]
		if (DistanceSq < abs(radius * radius))
		{
			float Distance = sqrt(DistanceSq);

			//normalize
			PointLightDirection /= Distance;

			float du = Distance / (1 - DistanceSq / (radius * radius - 1));

			float denom = du / abs(radius) + 1;

			//The attenuation is the falloff of the light depending on distance basically
			float attenuation = 1 / (denom * denom);

			diffuseLight += CalcDiffuseLight(input.Normal, PointLightDirection, PointLightColor[i], PointLightIntensity[i]) * attenuation;

			specularLight += CalcSpecularLight(input.Normal, PointLightDirection, cameraDirection, PointLightColor[i], PointLightIntensity[i]) * attenuation;
		}
	}

	return (baseColor * diffuseLight) + baseColor * SunLightColor * SunLightIntensity;
}


technique BasicLightShader
{
	pass Pass1
	{
#if SM4
		VertexShader = compile vs_4_0_level_9_1 MainVS();
		PixelShader = compile ps_4_0_level_9_1 PS();
#else
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 PS();
#endif
	}
}
