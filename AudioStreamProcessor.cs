using Godot;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using System.Collections.Generic;

public class FixedSizedQueue<T> : Queue<T>
{
	public int Limit { get; set; }

	public new void Enqueue(T obj)
	{
		base.Enqueue(obj);
		while (Count > Limit) Dequeue();
	}
}

public partial class AudioStreamProcessor : AudioStreamPlayer
{
	private AudioEffectCapture _effect;
	private readonly FixedSizedQueue<Complex> _buffer = new();

	[Signal]
	public delegate void FFTEventHandler(float[] fftData);

	public const int BIN_COUNT = 2048;

	public override void _Ready()
	{
		var idx = AudioServer.GetBusIndex("Custom");
		AudioServer.SetBusMute(idx, true);
		_effect = AudioServer.GetBusEffect(idx, 0) as AudioEffectCapture;
		_buffer.Limit = 32768 / 4;
	}

	public override void _Process(double delta)
	{
		const int SAMPLE_RATE = 44100;
		const double BOUND_FREQ_LO = 99.0;
		const double BOUND_FREQ_HI = 3232.0;

		var buffer = _effect.GetBuffer(_effect.GetFramesAvailable());
		_effect.ClearBuffer();

		foreach (var sample in buffer)
		{
			_buffer.Enqueue(new Complex(sample.X, 0));
		}

		if (_buffer.Count < _buffer.Limit)
		{
			return;
		}

		var samples = _buffer.ToArray();

		for (int n = 0; n < samples.Length; n++)
		{
			// Apply Hanning window
			samples[n] = samples[n] * Complex.FromPolarCoordinates(1.0, Mathf.Pi * n / (samples.Length - 1));
		}

		Fourier.Forward(samples, FourierOptions.Matlab);
		var fftData = new float[BIN_COUNT];
		var positiveFftSize = _buffer.Limit / 2;

		for (var i = 0; i < BIN_COUNT; i++)
		{
			var exponent = (double)i / (BIN_COUNT - 1);
			var freqLo = BOUND_FREQ_LO * Mathf.Pow(BOUND_FREQ_HI / BOUND_FREQ_LO, exponent);
			var freqHi = BOUND_FREQ_LO * Mathf.Pow(BOUND_FREQ_HI / BOUND_FREQ_LO, exponent + (1.0 / (BIN_COUNT - 1)));
			var idxLo = (int)(freqLo * _buffer.Limit / SAMPLE_RATE);
			var idxHi = (int)(freqHi * _buffer.Limit / SAMPLE_RATE);
			idxLo = Mathf.Clamp(idxLo, 0, positiveFftSize - 1);
			idxHi = Mathf.Clamp(idxHi, 0, positiveFftSize);
			var maxMag = 0.0;

			for (int j = idxLo; j < idxHi; j++)
			{
				maxMag = Mathf.Max(maxMag, samples[j].Magnitude);
			}

			fftData[i] = (float)maxMag / positiveFftSize;
		}

		EmitSignal(SignalName.FFT, fftData);
	}
}
