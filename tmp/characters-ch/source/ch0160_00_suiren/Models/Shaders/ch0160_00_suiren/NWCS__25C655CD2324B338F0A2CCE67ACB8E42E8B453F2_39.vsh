precision mediump float;
layout(std140, column_major) uniform _ub_lights_vec4
{
	mediump vec4 _ub_lightDir;
	mediump vec4 _ub_lightPos;
	mediump vec4 _ub_lightColor;
	mediump vec4 _ub_lightParam0;
	mediump vec4 _ub_lightParam1;
	mediump vec4 _ub_lightShadowParam0;
} ub_lights;
layout(std140, column_major) uniform _ub_environment_vec4
{
	mediump vec4 _ub_fogColor;
	mediump vec4 _ub_fogParam0;
	mediump vec4 _ub_fogParam1;
	mediump vec4 _ub_mipParam;
} ub_fogs;
layout(std140, column_major) uniform _ub_camera_mat4x4
{
	highp mat4 _ub_viewMat;
	highp mat4 _ub_invViewMat;
	highp mat4 _ub_projMat;
	highp mat4 _ub_invProjMat;
	highp mat4 _ub_viewProjMat;
	highp mat4 _ub_invViewProjMat;
	highp mat4 _ub_normalMat;
	highp mat4 _ub_billboardMat;
} ub_cams;
layout(std140, column_major) uniform _ub_bones_mat4x4
{
	highp mat4 _ub_boneMat[256];
} ub_bones;
in vec2 uv0;
in highp vec3 position;
in vec4 boneIndex;
in vec4 boneWeight;
uniform highp mat4 u_modelMat;
uniform vec4 u_uvTransform[8];
out mediump vec4 v_uv0;
out mediump vec4 v_objNormal;
out lowp vec4 v_fogColor;
void main ()
{
  highp mat4 mat_1;
  highp vec4 tmp_position_2;
  highp vec4 tmpvar_3;
  tmpvar_3.w = 1.0;
  tmpvar_3.xyz = position;
  vec4 tmpvar_4;
  tmpvar_4 = (boneWeight * clamp ((boneIndex + vec4(1.0, 1.0, 1.0, 1.0)), 0.0, 1.0));
  vec4 tmpvar_5;
  tmpvar_5 = max (boneIndex, 0.0);
  mat_1 = (ub_bones._ub_boneMat[int(tmpvar_5.x)] * tmpvar_4.x);
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_5.y)] * tmpvar_4.y));
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_5.z)] * tmpvar_4.z));
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_5.w)] * tmpvar_4.w));
  tmp_position_2.xyz = (mat_1 * tmpvar_3).xyz;
  tmp_position_2.w = 1.0;
  highp vec4 tmpvar_6;
  tmpvar_6 = (ub_cams._ub_viewProjMat * (u_modelMat * tmp_position_2));
  v_fogColor = vec4(0.0, 0.0, 0.0, 0.0);
  gl_Position = tmpvar_6;
  highp vec2 result_7;
  highp vec3 tmpvar_8;
  tmpvar_8.z = 1.0;
  tmpvar_8.xy = uv0;
  result_7.x = dot (tmpvar_8, u_uvTransform[0].xyz);
  result_7.y = dot (tmpvar_8, u_uvTransform[1].xyz);
  v_uv0.xy = result_7;
  highp float mipLevel_9;
  mipLevel_9 = ((1.0 - (
    (ub_fogs._ub_mipParam.y - (tmpvar_6.w - ub_fogs._ub_mipParam.x))
   / 
    (ub_fogs._ub_mipParam.y - ub_fogs._ub_mipParam.x)
  )) * 8.0);
  highp float tmpvar_10;
  tmpvar_10 = clamp (mipLevel_9, 0.0, 8.0);
  mipLevel_9 = tmpvar_10;
  v_objNormal.w = tmpvar_10;
}


