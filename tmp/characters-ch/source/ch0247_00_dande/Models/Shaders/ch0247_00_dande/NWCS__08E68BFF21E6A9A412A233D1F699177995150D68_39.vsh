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
out mediump vec3 v_worldNormal;
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
  lowp vec4 tmpvar_18;
  highp vec4 rayDir_19;
  vec4 trans_20;
  vec4 tmpvar_21;
  tmpvar_21 = ub_cams._ub_invViewMat[3];
  trans_20.w = tmpvar_21.w;
  trans_20.xyz = (tmpvar_21.xyz * vec3((1.0/(tmpvar_21.w))));
  highp vec3 tmpvar_22;
  tmpvar_22 = (tmpvar_16.xyz - trans_20.xyz);
  rayDir_19.xyz = normalize(tmpvar_22);
  rayDir_19.w = max ((sqrt(
    dot (tmpvar_22, tmpvar_22)
  ) - ub_fogs._ub_fogParam0.y), 0.0);
  highp vec4 tmpvar_23;
  tmpvar_23.xyz = ub_fogs._ub_fogColor.xyz;
  tmpvar_23.w = (clamp (mix (
    (((ub_fogs._ub_fogColor.w * exp(
      (-(ub_fogs._ub_fogParam0.w) * (trans_20.y - ub_fogs._ub_fogParam0.z))
    )) * (1.0 - exp(
      ((-(rayDir_19.w) * rayDir_19.y) * ub_fogs._ub_fogParam0.w)
    ))) / (ub_fogs._ub_fogParam0.w * rayDir_19.y))
  , 1.0, 
    float(((1.0 - ub_fogs._ub_fogParam0.w) >= 1.0))
  ), 0.0, 1.0) * ub_fogs._ub_fogParam0.x);
  tmpvar_18 = tmpvar_23;
  v_fogColor = tmpvar_18;
  highp mat3 tmpvar_24;
  tmpvar_24[uint(0)] = u_modelVecMat[uint(0)].xyz;
  tmpvar_24[1u] = u_modelVecMat[1u].xyz;
  tmpvar_24[2u] = u_modelVecMat[2u].xyz;
  highp vec3 tmpvar_25;
  tmpvar_25 = normalize((tmpvar_24 * normalize(
    (tmpvar_15 * normal)
  )));
  mat3 tmpvar_26;
  mat4 tmpvar_27;
  tmpvar_27 = ub_cams._ub_normalMat;
  tmpvar_26[uint(0)] = tmpvar_27[uint(0)].xyz;
  tmpvar_26[1u] = tmpvar_27[1u].xyz;
  tmpvar_26[2u] = tmpvar_27[2u].xyz;
  highp vec2 tmpvar_28;
  tmpvar_28 = ((0.5 * normalize(
    (tmpvar_26 * tmpvar_25)
  ).xy) + 0.5);
  highp vec2 tmpvar_29;
  tmpvar_29.x = tmpvar_28.x;
  tmpvar_29.y = (1.0 - tmpvar_28.y);
  tmpvar_3.xyz = normalize((u_lvsObjSpaceCameraPosition.xyz - tmpvar_16.xyz));
  tmpvar_3.w = 0.0;
  gl_Position = tmpvar_17;
  highp vec2 result_30;
  highp vec3 tmpvar_31;
  tmpvar_31.z = 1.0;
  tmpvar_31.xy = uv0;
  result_30.x = dot (tmpvar_31, u_uvTransform[0].xyz);
  result_30.y = dot (tmpvar_31, u_uvTransform[1].xyz);
  v_uv0.xy = result_30;
  highp vec2 result_32;
  highp vec3 tmpvar_33;
  tmpvar_33.z = 1.0;
  tmpvar_33.xy = uv0;
  result_32.x = dot (tmpvar_33, u_uvTransform[2].xyz);
  result_32.y = dot (tmpvar_33, u_uvTransform[3].xyz);
  v_uv0.zw = result_32;
  highp vec2 result_34;
  highp vec3 tmpvar_35;
  tmpvar_35.z = 1.0;
  tmpvar_35.xy = tmpvar_29;
  result_34.x = dot (tmpvar_35, u_uvTransform[4].xyz);
  result_34.y = dot (tmpvar_35, u_uvTransform[5].xyz);
  v_uv1.xy = result_34;
  v_color = color;
  v_objNormal.xyz = tmpvar_25;
  highp float mipLevel_36;
  mipLevel_36 = ((1.0 - (
    (ub_fogs._ub_mipParam.y - (tmpvar_17.w - ub_fogs._ub_mipParam.x))
   / 
    (ub_fogs._ub_mipParam.y - ub_fogs._ub_mipParam.x)
  )) * 8.0);
  highp float tmpvar_37;
  tmpvar_37 = clamp (mipLevel_36, 0.0, 8.0);
  mipLevel_36 = tmpvar_37;
  v_objNormal.w = tmpvar_37;
  v_objLight = ub_lights._ub_lightDir.xyz;
  v_view = tmpvar_3;
  highp vec3 tmpvar_38;
  tmpvar_38 = (0.5 * (tmpvar_25 + vec3(1.0, 1.0, 1.0)));
  v_worldNormal = tmpvar_38;
}


