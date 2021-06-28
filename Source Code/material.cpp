#include "material.h"
#include "texture.h"
#include "application.h"
#include "extra/hdre.h"

StandardMaterial::StandardMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

StandardMaterial::~StandardMaterial()
{

}

void StandardMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);

	shader->setUniform("u_color", color);

	if (texture)
		shader->setUniform("u_texture", texture);
}

void StandardMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{
		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
}

void StandardMaterial::renderInMenu()
{
	ImGui::ColorEdit3("Color", (float*)&color); // Edit 3 floats representing a color
}

WireframeMaterial::WireframeMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/flat.fs");
}

WireframeMaterial::~WireframeMaterial()
{

}

void WireframeMaterial::render(Mesh* mesh, Matrix44 model, Camera * camera)
{
	if (shader && mesh)
	{
		glPolygonMode(GL_FRONT_AND_BACK, GL_LINE);

		//enable shader
		shader->enable();

		//upload material specific uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		glPolygonMode(GL_FRONT_AND_BACK, GL_FILL);
	}
}

VolumeMaterial::VolumeMaterial()
{
	color = vec4(1.f, 1.f, 1.f, 1.f);
	shader = Shader::Get("data/shaders/basic.vs", "data/shaders/volume.fs");
	quality = 50;
	brightness = 20;
	thr_value = 0.27;
	render_mode = 1;
	smoke_render_mode = 1;
	illumination_mode = 1;
	is_smoke = false;
}

VolumeMaterial::~VolumeMaterial()
{

}

void VolumeMaterial::setUniforms(Camera* camera, Matrix44 model)
{
	
	//upload node uniforms
	shader->setUniform("u_viewprojection", camera->viewprojection_matrix);
	shader->setUniform("u_camera_position", camera->eye);
	shader->setUniform("u_model", model);

	shader->setUniform("u_light_position", Application::instance->light_position);
	if (is_smoke)
	{
		shader = Shader::Get("data/shaders/basic.vs", "data/shaders/volume_smoke.fs");
		shader->setUniform("u_tf", Texture::Get("data/textures/tf_smoke.tga"));
	}
	else
	{
		shader = Shader::Get("data/shaders/basic.vs", "data/shaders/volume.fs");
		shader->setUniform("u_tf", Texture::Get("data/textures/tf_abdomen.tga"));
	}
	shader->setUniform("u_time", Application::instance->time);
	// Compute local camera position
	Matrix44 model_local = model;
	Vector3 u_local_camera_position = Vector3(0,0,0);

	if (model_local.inverse())
		u_local_camera_position = model_local * camera->eye;

	shader->setUniform("u_local_camera_position", u_local_camera_position);

	Vector3 translation = model.getTranslation();
	bool u_cam_inside = false;
	if (u_local_camera_position.x >= -1 && u_local_camera_position.x <= 1 && u_local_camera_position.y >= -1 && u_local_camera_position.y <= 1 && u_local_camera_position.z >= -1 && u_local_camera_position.z <= 1)
	{
		glCullFace(GL_FRONT);
		u_cam_inside = true;
	}
	else glCullFace(GL_BACK);

	shader->setUniform("u_inside", u_cam_inside);

	shader->setUniform("u_color", color);
	shader->setUniform("u_quality", quality);
	shader->setUniform("u_brightness", brightness);
	shader->setUniform("u_threshold_value", thr_value);

	//Render modes
	shader->setUniform("u_color_bool", render_mode == 1);
	shader->setUniform("u_value_tf_bool", render_mode == 2);
	shader->setUniform("u_normal_tf_bool", render_mode == 3);
	shader->setUniform("u_img_tf_bool", render_mode == 4);

	//Smoke render modes
	shader->setUniform("u_vf_only", smoke_render_mode == 1);
	shader->setUniform("u_vf_img_tf", smoke_render_mode == 2);

	//illumination modes
	shader->setUniform("u_no_illumination", illumination_mode == 1);
	shader->setUniform("u_local_illumination", illumination_mode == 2);
	shader->setUniform("u_global_illumination", illumination_mode == 3);


	if (texture)
		shader->setUniform("u_texture", texture);
}

void VolumeMaterial::render(Mesh* mesh, Matrix44 model, Camera* camera)
{
	if (mesh && shader)
	{


		//enable shader
		shader->enable();

		//upload uniforms
		setUniforms(camera, model);

		//do the draw call
		mesh->render(GL_TRIANGLES);

		//disable shader
		shader->disable();
	}
}

void VolumeMaterial::renderInMenu()
{
	ImGui::Checkbox("Smoke", &is_smoke);
	ImGui::SliderFloat("Threshold value", (float*)&thr_value, 0, 1);
	if (is_smoke)
	{
		ImGui::Text("1. VF Without TF \n2. VF with TF using image (gradient + valeu) (BAD PERFORMANCE)");
		ImGui::SliderInt("Smoke Render mode", (int*)&smoke_render_mode, 1, 2);
	}
	else
	{
		ImGui::Text("1. Single Color Render \n2. TF using volume value \n3. TF using gradient \n4. TF using image (gradient + value)");
		ImGui::SliderInt("Render mode", (int*)&render_mode, 1, 4);
	}
	ImGui::Text("1. No illumination \n2. Local illumination (BAD PERFORMANCE WITH SMOKE)\n3. Global illumination (BAD PERFORMANCE WITH SMOKE, VERY BAD WITH SMOKE + IMG TF)");
	ImGui::SliderInt("Illumination mode", (int*)&illumination_mode,1,3);
	ImGui::ColorEdit3("Color", (float*)&color); // Edit 3 floats representing a color
	ImGui::SliderFloat("Quality", (float*)&quality, 1.0, 100);
	ImGui::SliderFloat("Brightness", (float*)&brightness, 1.0, 100);
	
}