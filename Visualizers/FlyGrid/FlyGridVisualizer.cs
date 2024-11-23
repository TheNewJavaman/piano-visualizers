using System;
using Godot;

public partial class FlyGridVisualizer : MultiMeshInstance2D
{
    [Export]
    public AudioStreamProcessor AudioStreamProcessor { get; set; }

    private float[] _oldData;
    private float[] _newData;

    public override void _Ready()
    {
        AudioStreamProcessor.FFT += OnFFT;
    }

    public override void _Process(double delta)
    {
        const int RESOLUTION = 10;
        const float MARGIN = 40.0f;
        const float SCALE = 10.0f;
        const float HORIZON = 0.75f;

        if (_newData == null)
        {
            return;
        }

        if (_oldData == null)
        {
            _oldData = _newData;
            Multimesh.InstanceCount = _newData.Length * 2;
            return;
        }

        var alpha = Mathf.Pow(0.4f * (float)delta, 0.5f);
        for (int i = 0; i < _oldData.Length; i++)
        {
            _oldData[i] = Mathf.Lerp(_oldData[i], _newData[i], alpha);
        }

        var size = GetViewportRect().Size - new Vector2(MARGIN * 2, MARGIN * 2);
        var horizonY = HORIZON * size.Y + MARGIN;

        for (int i = 0; i < _oldData.Length; i += RESOLUTION)
        {
            var maxMag = _oldData[i];

            for (int j = i + 1; j < i + RESOLUTION && j < _oldData.Length; j++)
            {
                maxMag = Mathf.Max(maxMag, _oldData[j]);
            }

            var u = (float)i / _oldData.Length;
            var v = Mathf.Clamp(maxMag, 0.05f, 1.0f);
            v = (v - 0.05f) / (1.0f - 0.05f);

            // Primary instance
            var position = new Vector2(MARGIN + size.X * u, horizonY - size.Y * HORIZON * v);
            var opacity = Mathf.Pow(v, 1.0f);
            Multimesh.SetInstanceTransform2D(i, new Transform2D(0, new Vector2(SCALE, SCALE), 0, position));
            Multimesh.SetInstanceColor(i, new Color(1, 1, 1, opacity));

            // Reflection instance
            position.Y = horizonY + size.Y * (1 - HORIZON) * v;
            opacity *= 0.2f;
            Multimesh.SetInstanceTransform2D(i + _oldData.Length, new Transform2D(0, new Vector2(SCALE, SCALE), 0, position));
            Multimesh.SetInstanceColor(i + _oldData.Length, new Color(1, 1, 1, opacity));
        }
    }

    public void OnFFT(float[] fftData)
    {
        _newData = fftData;
    }
}
