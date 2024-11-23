shader_type canvas_item;

uniform sampler2D fft_texture;

void fragment() {
	float fft_mag = texture(fft_texture, UV).x;
	fft_mag = pow(fft_mag, 1.0);
	COLOR.rgb = vec3(fft_mag, 0.0, 0.0);
}