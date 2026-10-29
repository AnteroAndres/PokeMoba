precision mediump float;
layout(location=0) out lowp vec4 o_color0;
layout(location=1) out highp vec4 o_color1;
layout(location=2) out vec4 o_color2;
in mediump vec4 v_uv0;
in mediump vec4 v_uv1;
in mediump vec4 v_objNormal;
in mediump vec3 v_objLight;
in mediump vec3 v_worldNormal;
in lowp vec4 v_fogColor;
uniform sampler2D u_texture0;
uniform sampler2D u_texture1;
uniform sampler2D u_texture2;
uniform vec4 u_constantColor0;
uniform vec4 u_constantColor1;
uniform vec4 u_constantColor5;
uniform vec4 u_constantColorAdd;
uniform vec4 u_interpColor;
lowp vec4 s_texture_0;
lowp vec4 s_texture_1;
lowp vec4 s_answer;
void main ()
{
  s_texture_0 = textureLod (u_texture0, v_uv0.xy, v_objNormal.w);
  s_texture_1 = textureLod (u_texture1, v_uv0.zw, v_objNormal.w);
  s_texture_0.xyz = mix (s_texture_0.xyz, ((0.333333 * 
    ((s_texture_0.x + s_texture_0.y) + s_texture_0.z)
  ) * u_interpColor.xyz), u_interpColor.w);
  mediump float tmpvar_1;
  tmpvar_1 = max (dot (v_objNormal.xyz, -(v_objLight)), 0.0);
  s_answer.xyz = (s_texture_0.xyz * u_constantColor0.xyz);
  s_answer.w = (tmpvar_1 * tmpvar_1);
  lowp vec4 tmpvar_2;
  tmpvar_2 = clamp (s_answer, 0.0, 1.0);
  s_answer.xyz = (s_texture_1.xyz * tmpvar_2.www);
  s_answer.w = s_texture_0.w;
  s_answer.xyz = ((s_texture_1.xyz * textureLod (u_texture2, v_uv1.xy, v_objNormal.w).www) + clamp (s_answer, 0.0, 1.0).xyz);
  s_answer.w = (s_texture_0.w * s_texture_0.x);
  lowp vec4 tmpvar_3;
  tmpvar_3 = clamp (s_answer, 0.0, 1.0);
  s_answer.xyz = ((s_texture_0.xyz * tmpvar_3.xyz) + (tmpvar_2.xyz * (vec3(1.0, 1.0, 1.0) - tmpvar_3.xyz)));
  s_answer.w = s_texture_0.w;
  lowp vec4 tmpvar_4;
  tmpvar_4 = clamp (s_answer, 0.0, 1.0);
  s_answer.xyz = (tmpvar_4.xyz * u_constantColor1.xyz);
  s_answer.w = s_texture_0.w;
  s_answer.xyz = ((tmpvar_4.xyz * s_texture_1.www) + (clamp (s_answer, 0.0, 1.0).xyz * (vec3(1.0, 1.0, 1.0) - s_texture_1.www)));
  s_answer.w = (s_texture_0.w * s_texture_0.w);
  s_answer.xyz = (clamp (s_answer, 0.0, 1.0).xyz * u_constantColor5.xyz);
  s_answer.w = s_texture_0.w;
  lowp vec4 tmpvar_5;
  tmpvar_5 = clamp (s_answer, 0.0, 1.0);
  s_answer.w = tmpvar_5.w;
  s_answer.xyz = mix (tmpvar_5.xyz, u_interpColor.xyz, u_interpColor.w);
  o_color0.w = 1.0;
  o_color2.x = 1.0;
  o_color1.xy = v_worldNormal.xy;
  o_color1.z = gl_FragCoord.z;
  o_color2.y = 0.0;
  o_color1.w = 1.0;
  o_color2.w = 1.0;
  o_color2.z = 0.43;
  o_color0.xyz = (s_answer.xyz + (u_constantColorAdd.w * u_constantColorAdd.xyz));
  o_color0.xyz = mix (o_color0.xyz, v_fogColor.xyz, v_fogColor.w);
}


