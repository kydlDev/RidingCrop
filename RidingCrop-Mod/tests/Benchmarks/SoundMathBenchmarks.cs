using BenchmarkDotNet.Attributes;

namespace RidingCrop.Benchmarks;

/// <summary>The whip-crack sound pipeline: runs once per sound at start-up (comparison axis: clip length).</summary>
[MemoryDiagnoser]
public class SoundMathBenchmarks
{
    float[] _data;
    int _channels = 2, _frequency = 44100;

    [Params(44100, 441000)]
    public int Samples { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rnd = new System.Random(7);
        _data = new float[Samples];
        for (int i = 0; i < _data.Length; i++)
        {
            double env = i < 8000 ? 0.02 : System.Math.Exp(-(i - 8000) / 20000.0);
            _data[i] = (float)((rnd.NextDouble() * 2 - 1) * env);
        }
    }

    [Benchmark]
    public float Peak() => SoundMath.Peak(_data);

    [Benchmark]
    public int OnsetFrame() => SoundMath.OnsetFrame(_data, _channels, SoundMath.Peak(_data), SoundMath.OnsetFraction);

    [Benchmark]
    public float[] TrimAndNormalize() => SoundMath.TrimAndNormalize(_data, _channels, _frequency, 4000, 2.5f);

    [Benchmark]
    public float SoftLimit() => SoundMath.SoftLimit(0.93f);
}
