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
  tmpvar_6 = (u_modelMat * tmp_position_2);
  highp vec4 tmpvar_7;
  tmpvar_7 = (ub_cams._ub_viewProjMat * tmpvar_6);
  lowp vec4 tmpvar_8;
  highp vec4 rayDir_9;
  vec4 trans_10;
  vec4 tmpvar_11;
  tmpvar_11 = ub_cams._ub_invViewMat[3];
  trans_10.w = tmpvar_11.w;
  trans_10.xyz = (tmpvar_11.xyz * vec3((1.0/(tmpvar_11.w))));
  highp vec3 tmpvar_12;
  tmpvar_12 = (tmpvar_6.xyz - trans_10.xyz);
  rayDir_9.xyz = normalize(tmpvar_12);
  rayDir_9.w = max ((sqrt(
    dot (tmpvar_12, tmpvar_12)
  ) - ub_fogs._ub_fogParam0.y), 0.0);
  highp vec4 tmpvar_13;
  tmpvar_13.xyz = ub_fogs._ub_fogColor.xyz;
  tmpvar_13.w = (clamp (mix (
    (((ub_fogs._ub_fogColor.w * exp(
      (-(ub_fogs._ub_fogParam0.w) * (trans_10.y - ub_fogs._ub_fogParam0.z))
    )) * (1.0 - exp(
      ((-(rayDir_9.w) * rayDir_9.y) * ub_fogs._ub_fogParam0.w)
    ))) / (ub_fogs._ub_fogParam0.w * rayDir_9.y))
  , 1.0, 
    float(((1.0 - ub_fogs._ub_fogParam0.w) >= 1.0))
  ), 0.0, 1.0) * ub_fogs._ub_fogParam0.x);
  tmpvar_8 = tmpvar_13;
  v_fogColor = tmpvar_8;
  gl_Position = tmpvar_7;
  highp vec2 result_14;
  highp vec3 tmpvar_15;
  tmpvar_15.z = 1.0;
  tmpvar_15.xy = uv0;
  result_14.x = dot (tmpvar_15, u_uvTransform[0].xyz);
  result_14.y = dot (tmpvar_15, u_uvTransform[1].xyz);
  v_uv0.xy = result_14;
  highp float mipLevel_16;
  mipLevel_16 = ((1.0 - (
    (ub_fogs._ub_mipParam.y - (tmpvar_7.w - ub_fogs._ub_mipParam.x))
   / 
    (ub_fogs._ub_mipParam.y - ub_fogs._ub_mipParam.x)
  )) * 8.0);
  highp float tmpvar_17;
  tmpvar_17 = clamp (mipLevel_16, 0.0, 8.0);
  mipLevel_16 = tmpvar_17;
  v_objNormal.w = tmpvar_17;
}


