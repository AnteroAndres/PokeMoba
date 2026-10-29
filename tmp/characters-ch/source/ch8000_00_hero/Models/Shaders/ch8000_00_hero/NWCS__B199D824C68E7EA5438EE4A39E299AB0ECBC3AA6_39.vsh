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
in vec3 normal;
in highp vec3 position;
in vec4 boneIndex;
in vec4 boneWeight;
uniform highp mat4 u_modelMat;
uniform highp mat4 u_modelVecMat;
uniform vec4 u_uvTransform[8];
out mediump vec4 v_uv0;
out mediump vec4 v_uv1;
out mediump vec4 v_objNormal;
out mediump vec3 v_objLight;
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
  highp vec3 tmpvar_6;
  highp vec3 tmpvar_7;
  highp vec3 tmpvar_8;
  tmpvar_6 = mat_1[uint(0)].xyz;
  tmpvar_7 = mat_1[1u].xyz;
  tmpvar_8 = mat_1[2u].xyz;
  highp mat3 tmpvar_9;
  highp float tmpvar_10;
  highp float tmpvar_11;
  highp float tmpvar_12;
  tmpvar_10 = ((tmpvar_7.y * tmpvar_8.z) - (tmpvar_8.y * tmpvar_7.z));
  tmpvar_11 = ((tmpvar_7.x * tmpvar_8.z) - (tmpvar_8.x * tmpvar_7.z));
  tmpvar_12 = ((tmpvar_7.x * tmpvar_8.y) - (tmpvar_8.x * tmpvar_7.y));
  highp mat3 tmpvar_13;
  tmpvar_13[0].x = tmpvar_10;
  tmpvar_13[1].x = -(tmpvar_11);
  tmpvar_13[2].x = tmpvar_12;
  tmpvar_13[0].y = ((tmpvar_8.y * tmpvar_6.z) - (tmpvar_6.y * tmpvar_8.z));
  tmpvar_13[1].y = ((tmpvar_6.x * tmpvar_8.z) - (tmpvar_8.x * tmpvar_6.z));
  tmpvar_13[2].y = ((tmpvar_8.x * tmpvar_6.y) - (tmpvar_6.x * tmpvar_8.y));
  tmpvar_13[0].z = ((tmpvar_6.y * tmpvar_7.z) - (tmpvar_7.y * tmpvar_6.z));
  tmpvar_13[1].z = ((tmpvar_7.x * tmpvar_6.z) - (tmpvar_6.x * tmpvar_7.z));
  tmpvar_13[2].z = ((tmpvar_6.x * tmpvar_7.y) - (tmpvar_7.x * tmpvar_6.y));
  tmpvar_9 = (tmpvar_13 / ((
    (tmpvar_6.x * tmpvar_10)
   - 
    (tmpvar_6.y * tmpvar_11)
  ) + (tmpvar_6.z * tmpvar_12)));
  highp mat3 tmpvar_14;
  tmpvar_14[0].x = tmpvar_9[0].x;
  tmpvar_14[1].x = tmpvar_9[0].y;
  tmpvar_14[2].x = tmpvar_9[0].z;
  tmpvar_14[0].y = tmpvar_9[1].x;
  tmpvar_14[1].y = tmpvar_9[1].y;
  tmpvar_14[2].y = tmpvar_9[1].z;
  tmpvar_14[0].z = tmpvar_9[2].x;
  tmpvar_14[1].z = tmpvar_9[2].y;
  tmpvar_14[2].z = tmpvar_9[2].z;
  highp vec4 tmpvar_15;
  tmpvar_15 = (u_modelMat * tmp_position_2);
  highp vec4 tmpvar_16;
  tmpvar_16 = (ub_cams._ub_viewProjMat * tmpvar_15);
  lowp vec4 tmpvar_17;
  highp vec4 rayDir_18;
  vec4 trans_19;
  vec4 tmpvar_20;
  tmpvar_20 = ub_cams._ub_invViewMat[3];
  trans_19.w = tmpvar_20.w;
  trans_19.xyz = (tmpvar_20.xyz * vec3((1.0/(tmpvar_20.w))));
  highp vec3 tmpvar_21;
  tmpvar_21 = (tmpvar_15.xyz - trans_19.xyz);
  rayDir_18.xyz = normalize(tmpvar_21);
  rayDir_18.w = max ((sqrt(
    dot (tmpvar_21, tmpvar_21)
  ) - ub_fogs._ub_fogParam0.y), 0.0);
  highp vec4 tmpvar_22;
  tmpvar_22.xyz = ub_fogs._ub_fogColor.xyz;
  tmpvar_22.w = (clamp (mix (
    (((ub_fogs._ub_fogColor.w * exp(
      (-(ub_fogs._ub_fogParam0.w) * (trans_19.y - ub_fogs._ub_fogParam0.z))
    )) * (1.0 - exp(
      ((-(rayDir_18.w) * rayDir_18.y) * ub_fogs._ub_fogParam0.w)
    ))) / (ub_fogs._ub_fogParam0.w * rayDir_18.y))
  , 1.0, 
    float(((1.0 - ub_fogs._ub_fogParam0.w) >= 1.0))
  ), 0.0, 1.0) * ub_fogs._ub_fogParam0.x);
  tmpvar_17 = tmpvar_22;
  v_fogColor = tmpvar_17;
  highp mat3 tmpvar_23;
  tmpvar_23[uint(0)] = u_modelVecMat[uint(0)].xyz;
  tmpvar_23[1u] = u_modelVecMat[1u].xyz;
  tmpvar_23[2u] = u_modelVecMat[2u].xyz;
  highp vec3 tmpvar_24;
  tmpvar_24 = normalize((tmpvar_23 * normalize(
    (tmpvar_14 * normal)
  )));
  mat3 tmpvar_25;
  mat4 tmpvar_26;
  tmpvar_26 = ub_cams._ub_normalMat;
  tmpvar_25[uint(0)] = tmpvar_26[uint(0)].xyz;
  tmpvar_25[1u] = tmpvar_26[1u].xyz;
  tmpvar_25[2u] = tmpvar_26[2u].xyz;
  highp vec2 tmpvar_27;
  tmpvar_27 = ((0.5 * normalize(
    (tmpvar_25 * tmpvar_24)
  ).xy) + 0.5);
  highp vec2 tmpvar_28;
  tmpvar_28.x = tmpvar_27.x;
  tmpvar_28.y = (1.0 - tmpvar_27.y);
  gl_Position = tmpvar_16;
  highp vec2 result_29;
  highp vec3 tmpvar_30;
  tmpvar_30.z = 1.0;
  tmpvar_30.xy = uv0;
  result_29.x = dot (tmpvar_30, u_uvTransform[0].xyz);
  result_29.y = dot (tmpvar_30, u_uvTransform[1].xyz);
  v_uv0.xy = result_29;
  highp vec2 result_31;
  highp vec3 tmpvar_32;
  tmpvar_32.z = 1.0;
  tmpvar_32.xy = uv0;
  result_31.x = dot (tmpvar_32, u_uvTransform[2].xyz);
  result_31.y = dot (tmpvar_32, u_uvTransform[3].xyz);
  v_uv0.zw = result_31;
  highp vec2 result_33;
  highp vec3 tmpvar_34;
  tmpvar_34.z = 1.0;
  tmpvar_34.xy = tmpvar_28;
  result_33.x = dot (tmpvar_34, u_uvTransform[4].xyz);
  result_33.y = dot (tmpvar_34, u_uvTransform[5].xyz);
  v_uv1.xy = result_33;
  v_objNormal.xyz = tmpvar_24;
  highp float mipLevel_35;
  mipLevel_35 = ((1.0 - (
    (ub_fogs._ub_mipParam.y - (tmpvar_16.w - ub_fogs._ub_mipParam.x))
   / 
    (ub_fogs._ub_mipParam.y - ub_fogs._ub_mipParam.x)
  )) * 8.0);
  highp float tmpvar_36;
  tmpvar_36 = clamp (mipLevel_35, 0.0, 8.0);
  mipLevel_35 = tmpvar_36;
  v_objNormal.w = tmpvar_36;
  v_objLight = ub_lights._ub_lightDir.xyz;
}


