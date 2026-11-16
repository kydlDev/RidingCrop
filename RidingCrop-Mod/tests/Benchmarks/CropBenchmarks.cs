using BenchmarkDotNet.Attributes;

namespace RidingCrop.Benchmarks
{
    /// <summary>Crop stat formulas (asked when an item tooltip or a hit is built) and the whip-crack sound pipeline (once per sound at start-up; comparison axis: clip length).</summary>
    [MemoryDiagnoser]
    public class CropBenchmarks
    {
        [Benchmark]
        public float Blunt() => CropBalance.Blunt(3);

        [Benchmark]
        public float BluntAtQuality() => CropBalance.BluntAtQuality(3, 4);

        [Benchmark]
        public float BleedPerSecond() => CropBalance.BleedPerSecond(3);

        [Benchmark]
        public float StaggerMultiplier() => CropBalance.StaggerMultiplier(3);
    }

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
}
