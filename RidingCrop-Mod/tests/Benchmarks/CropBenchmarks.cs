using BenchmarkDotNet.Attributes;

namespace RidingCrop.Benchmarks;

/// <summary>Crop stat formulas, asked when an item tooltip or a hit is built.</summary>
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
