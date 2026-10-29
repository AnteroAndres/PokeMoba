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
in vec4 color;
in vec3 normal;
in highp vec3 position;
in vec4 boneIndex;
in vec4 boneWeight;
uniform highp mat4 u_modelMat;
uniform highp mat4 u_modelVecMat;
uniform highp vec4 u_lvsObjSpaceCameraPosition;
uniform vec4 u_uvTransform[8];
out mediump vec4 v_uv0;
out mediump vec4 v_uv1;
out lowp vec4 v_color;
out mediump vec4 v_objNormal;
out mediump vec3 v_objLight;
out mediump vec4 v_view;
out lowp vec4 v_fogColor;
void main ()
{
  highp mat4 mat_1;
  highp vec4 tmp_position_2;
  highp vec4 tmpvar_3;
  highp vec4 tmpvar_4;
  tmpvar_4.w = 1.0;
  tmpvar_4.xyz = position;
  vec4 tmpvar_5;
  tmpvar_5 = (boneWeight * clamp ((boneIndex + vec4(1.0, 1.0, 1.0, 1.0)), 0.0, 1.0));
  vec4 tmpvar_6;
  tmpvar_6 = max (boneIndex, 0.0);
  mat_1 = (ub_bones._ub_boneMat[int(tmpvar_6.x)] * tmpvar_5.x);
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_6.y)] * tmpvar_5.y));
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_6.z)] * tmpvar_5.z));
  mat_1 = (mat_1 + (ub_bones._ub_boneMat[int(tmpvar_6.w)] * tmpvar_5.w));
  tmp_position_2.xyz = (mat_1 * tmpvar_4).xyz;
  tmp_position_2.w = 1.0;
  highp vec3 tmpvar_7;
  highp vec3 tmpvar_8;
  highp vec3 tmpvar_9;
  tmpvar_7 = mat_1[uint(0)].xyz;
  tmpvar_8 = mat_1[1u].xyz;
  tmpvar_9 = mat_1[2u].xyz;
  highp mat3 tmpvar_10;
  highp float tmpvar_11;
  highp float tmpvar_12;
  highp float tmpvar_13;
  tmpvar_11 = ((tmpvar_8.y * tmpvar_9.z) - (tmpvar_9.y * tmpvar_8.z));
  tmpvar_12 = ((tmpvar_8.x * tmpvar_9.z) - (tmpvar_9.x * tmpvar_8.z));
  tmpvar_13 = ((tmpvar_8.x * tmpvar_9.y) - (tmpvar_9.x * tmpvar_8.y));
  highp mat3 tmpvar_14;
  tmpvar_14[0].x = tmpvar_11;
  tmpvar_14[1].x = -(tmpvar_12);
  tmpvar_14[2].x = tmpvar_13;
  tmpvar_14[0].y = ((tmpvar_9.y * tmpvar_7.z) - (tmpvar_7.y * tmpvar_9.z));
  tmpvar_14[1].y = ((tmpvar_7.x * tmpvar_9.z) - (tmpvar_9.x * tmpvar_7.z));
  tmpvar_14[2].y = ((tmpvar_9.x * tmpvar_7.y) - (tmpvar_7.x * tmpvar_9.y));
  tmpvar_14[0].z = ((tmpvar_7.y * tmpvar_8.z) - (tmpvar_8.y * tmpvar_7.z));
  tmpvar_14[1].z = ((tmpvar_8.x * tmpvar_7.z) - (tmpvar_7.x * tmpvar_8.z));
  tmpvar_14[2].z = ((tmpvar_7.x * tmpvar_8.y) - (tmpvar_8.x * tmpvar_7.y));
  tmpvar_10 = (tmpvar_14 / ((
    (tmpvar_7.x * tmpvar_11)
   - 
    (tmpvar_7.y * tmpvar_12)
  ) + (tmpvar_7.z * tmpvar_13)));
  highp mat3 tmpvar_15;
  tmpvar_15[0].x = tmpvar_10[0].x;
  tmpvar_15[1].x = tmpvar_10[0].y;
  tmpvar_15[2].x = tmpvar_10[0].z;
  tmpvar_15[0].y = tmpvar_10[1].x;
  tmpvar_15[1].y = tmpvar_10[1].y;
  tmpvar_15[2].y = tmpvar_10[1].z;
  tmpvar_15[0].z = tmpvar_10[2].x;
  tmpvar_15[1].z = tmpvar_10[2].y;
  tmpvar_15[2].z = tmpvar_10[2].z;
  highp vec4 tmpvar_16;
  tmpvar_16 = (u_modelMat * tmp_position_2);
  highp vec4 tmpvar_17;
  tmpvar_17 = (ub_cams._ub_viewProjMat * tmpvar_16);
  v_fogColor = vec4(0.0, 0.0, 0.0, 0.0);
  highp mat3 tmpvar_18;
  tmpvar_18[uint(0)] = u_modelVecMat[uint(0)].xyz;
  tmpvar_18[1u] = u_modelVecMat[1u].xyz;
  tmpvar_18[2u] = u_modelVecMat[2u].xyz;
  highp vec3 tmpvar_19;
  tmpvar_19 = normalize((tmpvar_18 * normalize(
    (tmpvar_15 * normal)
  )));
  mat3 tmpvar_20;
  mat4 tmpvar_21;
  tmpvar_21 = ub_cams._ub_normalMat;
  tmpvar_20[uint(0)] = tmpvar_21[uint(0)].xyz;
  tmpvar_20[1u] = tmpvar_21[1u].xyz;
  tmpvar_20[2u] = tmpvar_21[2u].xyz;
  highp vec2 tmpvar_22;
  tmpvar_22 = ((0.5 * normalize(
    (tmpvar_20 * tmpvar_19)
  ).xy) + 0.5);
  highp vec2 tmpvar_23;
  tmpvar_23.x = tmpvar_22.x;
  tmpvar_23.y = (1.0 - tmpvar_22.y);
  tmpvar_3.xyz = normalize((u_lvsObjSpaceCameraPosition.xyz - tmpvar_16.xyz));
  tmpvar_3.w = 0.0;
  gl_Position = tmpvar_17;
  highp vec2 result_24;
  highp vec3 tmpvar_25;
  tmpvar_25.z = 1.0;
  tmpvar_25.xy = uv0;
  result_24.x = dot (tmpvar_25, u_uvTransform[0].xyz);
  result_24.y = dot (tmpvar_25, u_uvTransform[1].xyz);
  v_uv0.xy = result_24;
  highp vec2 result_26;
  highp vec3 tmpvar_27;
  tmpvar_27.z = 1.0;
  tmpvar_27.xy = uv0;
  result_26.x = dot (tmpvar_27, u_uvTransform[2].xyz);
  result_26.y = dot (tmpvar_27, u_uvTransform[3].xyz);
  v_uv0.zw = result_26;
  highp vec2 result_28;
  highp vec3 tmpvar_29;
  tmpvar_29.z = 1.0;
  tmpvar_29.xy = tmpvar_23;
  result_28.x = dot (tmpvar_29, u_uvTransform[4].xyz);
  result_28.y = dot (tmpvar_29, u_uvTransform[5].xyz);
  v_uv1.xy = result_28;
  v_color = color;
  v_objNormal.xyz = tmpvar_19;
  highp float mipLevel_30;
  mipLevel_30 = ((1.0 - (
    (ub_fogs._ub_mipParam.y - (tmpvar_17.w - ub_fogs._ub_mipParam.x))
   / 
    (ub_fogs._ub_mipParam.y - ub_fogs._ub_mipParam.x)
  )) * 8.0);
  highp float tmpvar_31;
  tmpvar_31 = clamp (mipLevel_30, 0.0, 8.0);
  mipLevel_30 = tmpvar_31;
  v_objNormal.w = tmpvar_31;
  v_objLight = ub_lights._ub_lightDir.xyz;
  v_view = tmpvar_3;
}


