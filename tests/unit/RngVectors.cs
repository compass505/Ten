using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;

namespace Ten.Tests.Unit;

/// <summary>
/// tests/vectors/rng.json の読み手。
/// docs/40_test/harness.md 2 節: 「これを正とする。C# 実装がこれと 1 件でも食い違ったら
/// NFR-004 が成立しない」
/// </summary>
internal sealed record RngVectorFile(
    [property: JsonPropertyName("hash")] IReadOnlyList<HashVector> Hash,
    [property: JsonPropertyName("milli")] IReadOnlyList<MilliVector> Milli,
    [property: JsonPropertyName("range")] IReadOnlyList<RangeVector> Range,
    [property: JsonPropertyName("uniformityCheck")] UniformityVector UniformityCheck)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal static RngVectorFile Load()
    {
        var path = RepoPaths.RngVectors;
        Assert.That(File.Exists(path), Is.True,
            $"参照ベクタが見つからない: {path}。csproj のコピー設定を確認する");

        var loaded = JsonSerializer.Deserialize<RngVectorFile>(File.ReadAllText(path), Options);
        Assert.That(loaded, Is.Not.Null, $"参照ベクタを読めない: {path}");
        return loaded!;
    }
}

internal sealed record HashVector(
    [property: JsonPropertyName("seed")] string Seed,
    [property: JsonPropertyName("purpose")] int Purpose,
    [property: JsonPropertyName("ordinal")] int Ordinal,
    [property: JsonPropertyName("expected")] uint Expected);

internal sealed record MilliVector(
    [property: JsonPropertyName("seed")] string Seed,
    [property: JsonPropertyName("purpose")] int Purpose,
    [property: JsonPropertyName("ordinal")] int Ordinal,
    [property: JsonPropertyName("expected")] int Expected);

internal sealed record RangeVector(
    [property: JsonPropertyName("seed")] string Seed,
    [property: JsonPropertyName("purpose")] int Purpose,
    [property: JsonPropertyName("ordinal")] int Ordinal,
    [property: JsonPropertyName("max")] int Max,
    [property: JsonPropertyName("expected")] int Expected);

internal sealed record UniformityVector(
    [property: JsonPropertyName("max")] int Max,
    [property: JsonPropertyName("n")] int N,
    [property: JsonPropertyName("counts")] IReadOnlyList<int> Counts,
    [property: JsonPropertyName("expectedEach")] int ExpectedEach);
