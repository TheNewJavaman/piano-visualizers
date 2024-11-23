using Godot;

public partial class DebugVisualizer : MeshInstance2D
{
	[Export]
	public AudioStreamProcessor AudioStreamProcessor { get; set; }

	private Image _image;
	private ImageTexture _texture;

	public override void _Ready()
	{
		_image = Image.CreateEmpty(AudioStreamProcessor.BIN_COUNT, 1, false, Image.Format.Rf);
		_texture = ImageTexture.CreateFromImage(_image);
		AudioStreamProcessor.FFT += OnFFT;
	}

	public void OnFFT(float[] fftData)
	{
		for (int i = 0; i < fftData.Length; i++)
		{
			_image.SetPixel(i, 0, new Color(fftData[i], 0, 0));
		}
		_texture.Update(_image);
		(Material as ShaderMaterial).SetShaderParameter("fft_texture", _texture);
	}
}
