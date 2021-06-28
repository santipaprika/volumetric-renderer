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

//threshold value to render
uniform float u_threshold_value;

//render mode bools
uniform bool u_color_bool;
uniform bool u_value_tf_bool;
uniform bool u_normal_tf_bool;
uniform bool u_img_tf_bool;

//illumination mode bools
uniform bool u_global_illumination;
uniform bool u_local_illumination;

//Inside or outside the volume
uniform bool u_inside;

//Optional to use
uniform float u_quality;
uniform float u_brightness;
uniform vec4 u_color;

float random (vec2 st) {
    return fract(sin(dot(st.xy,vec2(12.9898,78.233)))*43758.5453123);
}

void main()
{
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
		vec3 position_text = (position + 1)/2.0;
		color_i = texture3D(u_texture, position_text);
		float value = color_i.x;
		
		if (value < u_threshold_value)
		{
			position += step;
			if ((position.x > 1) || (position.y > 1) || (position.z > 1) || (position.x < -1) || (position.y < -1) || (position.z < -1))
				break;
			if (color_acc.a > 0.98)
				break;
			continue;
		}
		
		//A1 color computation
		if (u_color_bool) color_i = vec4(u_color.x,u_color.y, u_color.z, pow(value,2));
		
		//Value TF
		if (u_value_tf_bool)
		{
			if (color_i.x < 0.25) color_i = vec4(1,0,0,0.05);
			else if (color_i.x < 0.30) color_i = vec4(0,1,0,0.2);
			else color_i = vec4(1,1,1,0.7);
		}
		
		vec3 grad_pos = vec3(0);
		float grad_mag = 0;
		
		if ( u_img_tf_bool || u_normal_tf_bool || u_local_illumination )
		{
			//Isosurface normal obtained by gradient computation (we divide step by 2 because we are in texture coordinates)
			float pos_text_dx = texture3D(u_texture, position_text + vec3(step_len/2,0,0)).x - texture3D(u_texture, position_text - vec3(step_len/2,0,0)).x;
			float pos_text_dy = texture3D(u_texture, position_text + vec3(0,step_len/2,0)).x - texture3D(u_texture, position_text - vec3(0,step_len/2,0)).x;
			float pos_text_dz = texture3D(u_texture, position_text + vec3(0,0,step_len/2)).x - texture3D(u_texture, position_text - vec3(0,0,step_len/2)).x;
			
			grad_pos = vec3(pos_text_dx, pos_text_dy, pos_text_dz) / (step_len);
			grad_mag = length(grad_pos);
		}
		
		//Image TF
		if ( u_img_tf_bool )
		{
			float grad_mag_text = length( (grad_pos + 1) / (2 * sqrt(3)) );
			color_i = texture2D(u_tf, vec2(value, grad_mag_text));
		}
		
		//Isosurface normal TF
		if (u_normal_tf_bool)
		{
			if (grad_mag < 2) color_i = vec4(1,0,0,0.04);
			else if (grad_mag < 9) color_i = vec4(0,1,0,0.15);
			else color_i = vec4(1,1,1,1);
		}
		
		vec3 point_light_vec = normalize(u_light_position - position);
		//Global illumination
		if(value > 0.3 && u_global_illumination)
		{
			
			vec3 position_aux = position;
			vec3 step_to_light = 3 * point_light_vec / u_quality;	//larger than step vector
			position_aux += step_to_light;
			float value_acc = 0;
			
			for (int j = 0; j < 100; j++)
			{
				vec3 position_text_aux = (position_aux + 1)/2.0;
				value_acc += texture3D(u_texture, position_text_aux).x;

				if ((position_aux.x > 1) || (position_aux.y > 1) || (position_aux.z > 1) || (position_aux.x < -1) || (position_aux.y < -1) || (position_aux.z < -1))
					break;
				if (value_acc > 0.98)
					break;
				position_aux += step_to_light;
			}
			color_i.rgb *= vec3(1-0.5*value_acc);
			//vec3 point_light_vec_norm = normalize(point_light_vec);
			
		}
		else if (u_local_illumination)
		{
			vec3 pos_normal = normalize(grad_pos);
			color_i.rgb *= vec3((dot(point_light_vec, pos_normal) + 1) / 2);
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