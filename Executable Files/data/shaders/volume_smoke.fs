varying vec3 v_position;
varying vec3 v_world_position;

//Texture space is [0, 1]
uniform sampler3D u_texture;
uniform sampler2D u_tf;

//Camera position
uniform vec3 u_camera_position;
uniform vec3 u_local_camera_position;	//You can use this now for the algorithm, in the assigment you will be responsible to compute it

//Light position
uniform vec3 u_light_position;

//Inside or outside the volume
uniform bool u_inside;

//threshold value to render
uniform float u_threshold_value;

//render mode bools
uniform bool u_vf_only;
uniform bool u_vf_img_tf;

//illumination mode bools
uniform bool u_global_illumination;
uniform bool u_local_illumination;

//Optional to use
uniform float u_quality;
uniform float u_brightness;
uniform vec4 u_color;

uniform vec2 u_resolution;
uniform vec2 u_mouse;
uniform float u_time;

#ifdef GL_ES
precision mediump float;
#endif

float hash(float n) { return fract(sin(n) * 1e4); }
float hash(vec2 p) { return fract(1e4 * sin(17.0 * p.x + p.y * 0.1) * (0.1 + abs(sin(p.y * 13.0 + p.x)))); }

float random (vec2 st) {
    return fract(sin(dot(st.xy,vec2(12.9898,78.233)))*43758.5453123);
}

float noise(vec3 x) {
    const vec3 step = vec3(110, 241, 171);

    vec3 i = floor(x);
    vec3 f = fract(x);
 
    // For performance, compute the base input to a 1D hash from the integer part of the argument and the 
    // incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

    vec3 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
               mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}

#define NUM_NOISE_OCTAVES 6

float fbm(vec3 x) {
	float v = 0.0;
	float a = 0.5;
	vec3 shift = vec3(100);
	for (int i = 0; i < NUM_NOISE_OCTAVES; ++i) {
		v += a * noise(x);
		x = x * 2.0 + shift;
		a *= 0.5;
	}
	return v;
}

float computeSmoke(vec3 position)
{
	
		vec3 st = (position + 1)/2.0;
		// st += st * abs(sin(u_time*0.1)*3.0);
		vec3 color = vec3(0.0);

		vec3 q = vec3(0.);
		q.x = fbm( st + 0.00*u_time);
		q.y = fbm( st + vec3(1.0));
		q.z = fbm( st - vec3(1.0));

		vec3 r = vec3(0.);
		r.x = fbm( st + 1.0*q + vec3(1.7,9.2,19.2)+ 0.15*u_time );
		r.y = fbm( st + 1.0*q + vec3(8.3,2.8,-1.51)+ 0.126*u_time);
		r.z = fbm( st + 1.0*q + vec3(71.1,10.91,-3.2)+ 0.195*u_time);

		float f = fbm(st+r);

		color = mix(vec3(0.101961,0.619608,0.666667),
                vec3(0.666667,0.666667,0.498039),
                clamp((f*f)*4.0,0.0,1.0));

		color = mix(color,
                vec3(0,0,0.164706),
                clamp(length(q),0.0,1.0));

		color = mix(color,
                vec3(0.666667,1,1),
                clamp(length(r.x),0.0,1.0));

		color = (f*f*f+.6*f*f+.5*f)*color;
		
		//Distance factor
		//color.x *= 1/(1+pow(length(distance(position,vec3(0,0,0))),3));
		
		return color.x;
}

vec3 computeGradient(vec3 position, float step_len)
{
	vec3 grad = vec3(0);

	vec3 pos_prev = position - vec3(step_len, 0, 0);
	vec3 pos_next = position + vec3(step_len, 0, 0);
	grad.x = (computeSmoke(pos_next) - computeSmoke(pos_prev)) / (2*step_len);
		
	pos_prev = position - vec3(0, step_len, 0);
	pos_next = position + vec3(0, step_len, 0);
	grad.y = (computeSmoke(pos_next) - computeSmoke(pos_prev)) / (2*step_len);
		
	pos_prev = position - vec3(0, 0, step_len);
	pos_next = position + vec3(0, 0, step_len);
	grad.z = (computeSmoke(pos_next) - computeSmoke(pos_prev)) / (2*step_len);
		
	return grad;
}

void main() {
	
	vec3 dir = normalize(v_position - u_local_camera_position);
	vec3 step = dir/u_quality;
	float step_len = length(step);
	
    float rnd = random( gl_FragCoord.xy);	//Jittering
	vec3 position = v_position + rnd * step;
	
	if (u_inside)
		position = u_local_camera_position + rnd * step;
	
	vec4 color_i = vec4(0,0,0,0);
	vec4 color_acc = vec4(0,0,0,0);
		
		
	for (int i=0; i<200; i++)
	{
		//compute value
		float value = computeSmoke(position);
		
		if (value < u_threshold_value)
		{
			position += step;
			if ((position.x > 1) || (position.y > 1) || (position.z > 1) || (position.x < -1) || (position.y < -1) || (position.z < -1))
				break;
			if (color_acc.a > 0.98)
				break;
			continue;
		}
		
		//those are the only modes that require gradient information ---------
		
		vec3 gradient = vec3(0);
		float grad_mag_text = 0;
		vec3 pos_normal = vec3(0);
		
		if (u_local_illumination || u_vf_img_tf)
		{
			gradient = computeGradient(position, step_len);
			grad_mag_text = length( (gradient + 1) / (2 * sqrt(3)) );
			pos_normal = normalize(gradient);
		}
		
		//using image as tf --------------------------------------------------
		
		if (u_vf_img_tf)
			color_i = texture2D(u_tf, vec2(value, grad_mag_text));
		else
			color_i.rgb = u_color.rgb;
		
		color_i.a = pow(value,2);;
		//color_i = vec4(color_i.rgb, pow(color_i.a,2));
		color_i.a *= 1/(1+17*pow(length(distance(position,vec3(0,0,0))),3));
		
		
		// ILLUMINATION ------------------------------------------------------
		
		vec3 point_light_vec = normalize(u_light_position - position);

		//Local illumination
		if (u_local_illumination)
		{
			vec3 pos_normal = normalize(gradient);
			color_i.rgb *= vec3((dot(point_light_vec, pos_normal) + 1) / 2);
		}
		
		//Global illumination
		else if(value > 0.17 && u_global_illumination)
		{
			
			vec3 position_aux = position;
			vec3 step_to_light = 3 * point_light_vec / u_quality;	//larger than step vector
			position_aux += step_to_light;
			float value_acc = 0;
			
			for (int j = 0; j < 100; j++)
			{
				value_acc += computeSmoke(position_aux);

				if ((position_aux.x > 1) || (position_aux.y > 1) || (position_aux.z > 1) || (position_aux.x < -1) || (position_aux.y < -1) || (position_aux.z < -1))
					break;
				if (value_acc > 0.98)
					break;
				position_aux += step_to_light;
			}
			color_i.rgb *= vec3(1-0.6*value_acc);			
		}
		
		
		color_i.rgb = color_i.rgb * color_i.a;
		color_acc += length(step) * color_i * (1.0 - color_acc.a + u_brightness/15.0);
	
		position += step;
		
		if ((position.x > 1) || (position.y > 1) || (position.z > 1) || (position.x < -1) || (position.y < -1) || (position.z < -1))
			break;
		if (color_acc.a > 0.98)
			break;
	}
	
	gl_FragColor = color_acc;
}