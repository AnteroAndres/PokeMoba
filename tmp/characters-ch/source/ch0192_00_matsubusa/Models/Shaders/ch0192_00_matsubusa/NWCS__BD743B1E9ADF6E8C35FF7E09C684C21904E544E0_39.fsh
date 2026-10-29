precision mediump float;
layout(location=0) out lowp vec4 o_color0;
layout(location=1) out vec4 o_color1;
layout(location=2) out vec4 o_color2;
in mediump vec4 v_uv0;
in mediump vec4 v_objNormal;
in lowp vec4 v_fogColor;
uniform sampler2D u_texture0;
uniform vec4 u_constantColor5;
uniform vec4 u_constantColorAdd;
uniform vec4 u_interpColor;
lowp vec4 s_texture_0;
lowp vec4 s_answer;
void main ()
{
  s_texture_0 = textureLod (u_texture0, v_uv0.xy, v_objNormal.w);
  s_texture_0.xyz = mix (s_texture_0.xyz, ((0.333333 * 
    ((s_texture_0.x + s_texture_0.y) + s_texture_0.z)
  ) * u_interpColor.xyz), u_interpColor.w);
  s_answer.xyz = (s_texture_0.xyz * u_constantColor5.xyz);
  s_answer.w = (s_texture_0.w * s_texture_0.w);
  lowp vec4 tmpvar_1;
  tmpvar_1 = clamp (s_answer, 0.0, 1.0);
  s_answer.w = tmpvar_1.w;
  s_answer.xyz = mix (tmpvar_1.xyz, u_interpColor.xyz, u_interpColor.w);
  o_color0.w = 1.0;
  o_color2.x = 0.0;
  o_color1.xy = vec2(0.0, 0.0);
  o_color1.z = 1.0;
  o_color2.y = 0.0;
  o_color1.w = 0.0;
  o_color2.w = 0.0;
  o_color2.z = 0.3;
  o_color0.xyz = (s_answer.xyz + (u_constantColorAdd.w * u_constantColorAdd.xyz));
  o_color0.xyz = mix (o_color0.xyz, v_fogColor.xyz, v_fogColor.w);
}


