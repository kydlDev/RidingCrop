using System;
using System.Linq;
using FsCheck;

namespace RidingCrop.Tests.Unit;

/// <summary>Property tests (FsCheck): the crop numbers only grow with the tier, and the sound pipeline never leaves the [-1, 1] range or lengthens a clip.</summary>
[TestFixture]
public class CropSoundPropertyTests
{
    static readonly Gen<float[]> Samples = Gen.Choose(1, 300).SelectMany(n => Gen.ArrayOf(n, Props.GameFloat(2f)));
    static readonly Gen<float> Amplified = Props.GameFloat(20f);

    [Test]
    public void Damage_GrowsWithEveryTier()
    {
        for (int t = 1; t < CropBalance.Tiers; t++)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CropBalance.Blunt(t), Is.GreaterThan(CropBalance.Blunt(t - 1)), "blunt tier " + t);
                Assert.That(CropBalance.BleedPerSecond(t), Is.GreaterThan(CropBalance.BleedPerSecond(t - 1)), "bleed tier " + t);
                Assert.That(CropBalance.Durability(t), Is.GreaterThan(CropBalance.Durability(t - 1)), "durability tier " + t);
            }
        }
    }

    [Test]
    public void BluntAtQuality_GrowsWithQuality()
    {
        Props.Check(Prop.ForAll(Gen.Choose(0, CropBalance.Tiers - 1).ToArbitrary(), Gen.Choose(1, CropBalance.MaxQuality - 1).ToArbitrary(), (tier, q) =>
            CropBalance.BluntAtQuality(tier, q + 1) > CropBalance.BluntAtQuality(tier, q)));
    }

    [Test]
    public void SoftLimit_NeverReachesOne_IsMonotonic_AndLeavesQuietSamplesAlone()
    {
        Props.Check(Prop.ForAll(Amplified.ToArbitrary(), Amplified.ToArbitrary(), (a, b) =>
        {
            float x = Math.Abs(a), y = Math.Abs(b);
            float lo = Math.Min(x, y), hi = Math.Max(x, y);
            bool bounded = SoundMath.SoftLimit(hi) <= 1.0001f;
            bool monotonic = SoundMath.SoftLimit(lo) <= SoundMath.SoftLimit(hi) + 1e-6f;
            bool quiet = x > SoundMath.LimiterKnee || Math.Abs(SoundMath.SoftLimit(x) - x) < 1e-6f;
            return bounded && monotonic && quiet;
        }));
    }

    [Test]
    public void Peak_IsTheLargestMagnitude()
    {
        Props.Check(Prop.ForAll(Samples.ToArbitrary(), data =>
        {
            float p = SoundMath.Peak(data);
            return p >= 0f && data.All(v => Math.Abs(v) <= p + 1e-6f) && data.Any(v => Math.Abs(Math.Abs(v) - p) < 1e-6f);
        }));
    }

    [Test]
    public void TrimAndNormalize_StaysInRange_AndOnlyShortensTheClip()
    {
        Props.Check(Prop.ForAll(Samples.ToArbitrary(), Gen.Choose(1, 2).ToArbitrary(), Props.GameFloat(5f).ToArbitrary(), (data, channels, gain) =>
        {
            int frames = data.Length / channels;
            if (frames == 0) return true;
            int start = frames / 3;
            float[] o = SoundMath.TrimAndNormalize(data.Take(frames * channels).ToArray(), channels, 44100, start, gain);
            return o.Length == (frames - start) * channels && o.All(v => v >= -1f && v <= 1f);
        }));
    }

    [Test]
    public void OnsetFrame_IsInsideTheClip()
    {
        Props.Check(Prop.ForAll(Samples.ToArbitrary(), Gen.Choose(1, 2).ToArbitrary(), (data, channels) =>
        {
            int f = SoundMath.OnsetFrame(data, channels, SoundMath.Peak(data), SoundMath.OnsetFraction);
            return f >= 0 && f <= data.Length / channels;
        }));
    }
}
