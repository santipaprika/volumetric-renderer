
uniform vec4 u_color;
varying vec3 v_position;

void main()
{
	gl_FragColor = vec4(u_color.x, u_color.y, u_color.z, u_color.a);
}
