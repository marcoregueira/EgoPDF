using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Ego.PDF.Samples;
using Xunit;

// Sample generators in this suite read shared font caches and image
// decoders that aren't currently safe to drive in parallel; cross-class
// runs were producing intermittent byte hash mismatches. The smoke and
// baseline tests together take ~2 s, so serialising the whole assembly
// is a cheaper fix than chasing the shared mutable state right now.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace Ego.Pdf.Test
{
    /// <summary>
    /// Byte-hash regression for every shipping sample. Each test generates
    /// the sample's PDF, strips the (clock-driven) /CreationDate field
    /// from the bytes, hashes the result with SHA-256 and compares against
    /// a baseline stored in <c>Baselines/{name}.sha256</c>.
    ///
    /// First-time setup or accepted refactors: set the environment
    /// variable <c>EGOPDF_UPDATE_BASELINES=1</c> and run the suite once.
    /// The tests will write each <c>{name}.sha256</c> + <c>{name}.pdf</c>
    /// pair and pass. Subsequent runs without the variable enforce parity.
    ///
    /// Limitations:
    ///  - This is a byte hash, not a pixel hash. A change to the font
    ///    subset selection or to the order of PDF objects will trip the
    ///    test even if the rendered page is visually identical.
    ///  - When a test fails, the new canonical bytes are dumped under
    ///    <c>actual-baselines/</c> in the test bin directory so you can
    ///    diff them against <c>Baselines/{name}.pdf</c> with any viewer.
    /// </summary>
    public class VisualBaselineTests
    {
        private const string UpdateEnvVar = "EGOPDF_UPDATE_BASELINES";

        [Fact] public void Baseline_Sample1()                => Run("Sample1",                  _ => Sample1.GetSample(null));
        [Fact] public void Baseline_Sample2_png()            => Run("Sample2_png",              p => Sample2.GetSample(null, Path.Combine(p, "logo.png")));
        [Fact] public void Baseline_Sample2_alpha()          => Run("Sample2_alpha",            p => Sample2.GetSample(null, Path.Combine(p, "3d_down.png")));
        [Fact] public void Baseline_Sample2_jpg()            => Run("Sample2_jpg",              p => Sample2.GetSample(null, Path.Combine(p, "gift.jpg")));
        [Fact] public void Baseline_Sample3()                => Run("Sample3",                  p => Sample3.GetSample(null, p));
        [Fact] public void Baseline_Sample4()                => Run("Sample4",                  p => Sample4.GetSample(null, p));
        [Fact] public void Baseline_Sample5()                => Run("Sample5",                  p => Sample5.GetSample(null, p));
        [Fact] public void Baseline_Sample6()                => Run("Sample6",                  p => Sample6.GetSample(null, p));
        [Fact] public void Baseline_Sample7()                => Run("Sample7",                  _ => Sample7.GetSample(null));
        [Fact] public void Baseline_Sample8()                => Run("Sample8",                  p => Sample8.GetSample(null, p));
        [Fact] public void Baseline_Sample8b()               => Run("Sample8b",                 p => Sample8b.GetSample(null, p));
        [Fact] public void Baseline_Sample8c()               => Run("Sample8c",                 p => Sample8c.GetSample(null, p));
        [Fact] public void Baseline_Sample9()                => Run("Sample9",                  p => Sample9.GetSample(null, p));
        [Fact] public void Baseline_SampleMarkdown()         => Run("SampleMarkdown",           _ => SampleMarkdown.GetSample(null));
        [Fact] public void Baseline_SamplePhotovoltaic()     => Run("SamplePhotovoltaic",       p => SamplePhotovoltaic.GetSample(null, p));
        [Fact] public void Baseline_SampleZebra_horizontal() => Run("SampleZebra_horizontal",   p => SampleZebra.GetSampleHorizontalShipping(null, p));
        [Fact] public void Baseline_SampleZebra_vertical1()  => Run("SampleZebra_vertical1",    p => SampleZebra.GetSampleVertical1(null, p));
        [Fact] public void Baseline_SampleZebra_barcodes()   => Run("SampleZebra_barcodes",     p => SampleZebra.GetSampleBarcodes(null, p));

        private static void Run(string name, Func<string, Stream> factory)
        {
            var assets = AssemblyDir();
            var stream = factory(assets);
            stream.Seek(0, SeekOrigin.Begin);
            var canonical = Canonicalize(ReadAll(stream));
            var actualHash = Sha256Hex(canonical);

            var baselinesDir = BaselinesDir();
            Directory.CreateDirectory(baselinesDir);
            var hashPath = Path.Combine(baselinesDir, name + ".sha256");
            var pdfPath  = Path.Combine(baselinesDir, name + ".pdf");
            var updating = string.Equals(Environment.GetEnvironmentVariable(UpdateEnvVar), "1",
                                         StringComparison.Ordinal);

            if (updating)
            {
                File.WriteAllText(hashPath, actualHash);
                File.WriteAllBytes(pdfPath, canonical);
                return;
            }

            if (!File.Exists(hashPath))
            {
                File.WriteAllText(hashPath, actualHash);
                File.WriteAllBytes(pdfPath, canonical);
                Assert.Fail(
                    $"No baseline existed for {name}. Wrote {hashPath} and {pdfPath}. " +
                    $"Inspect the PDF, commit it if it looks right, and re-run the suite.");
            }

            var expected = File.ReadAllText(hashPath).Trim();
            if (expected == actualHash) return;

            var actualDir = Path.Combine(assets, "actual-baselines");
            Directory.CreateDirectory(actualDir);
            var actualPdfPath = Path.Combine(actualDir, name + ".actual.pdf");
            File.WriteAllBytes(actualPdfPath, canonical);

            Assert.Fail(
                $"Baseline mismatch for {name}." + Environment.NewLine +
                $"  expected hash : {expected}" + Environment.NewLine +
                $"  actual hash   : {actualHash}" + Environment.NewLine +
                $"  actual PDF    : {actualPdfPath}" + Environment.NewLine +
                $"  baseline PDF  : {pdfPath}" + Environment.NewLine +
                $"  to accept the change, set {UpdateEnvVar}=1 and re-run.");
        }

        /// <summary>
        /// Strip the parts of the PDF that depend on wall-clock time so
        /// the hash is stable. Right now that's only /CreationDate.
        /// Latin1 is a 1:1 byte-string mapping (every byte 0..255 is a
        /// codepoint) so the regex doesn't garble any binary stream
        /// payload it happens to touch.
        /// </summary>
        private static byte[] Canonicalize(byte[] pdf)
        {
            var s = Encoding.Latin1.GetString(pdf);
            s = Regex.Replace(s, @"/CreationDate \(D:[^)]*\)", "/CreationDate (D:NORMALIZED)");
            return Encoding.Latin1.GetBytes(s);
        }

        private static byte[] ReadAll(Stream s)
        {
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        private static string Sha256Hex(byte[] data)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
        }

        private static string AssemblyDir()
            => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static string BaselinesDir()
        {
            // Walk up from the test bin directory until we find the
            // project's .csproj, then drop into a sibling "Baselines"
            // folder. Means the baselines live in the source tree and
            // are committed alongside the test code.
            var dir = new DirectoryInfo(AssemblyDir());
            while (dir != null && !dir.GetFiles("*.csproj").Any())
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException(
                    "Could not find a .csproj walking up from the test bin directory.");
            return Path.Combine(dir.FullName, "Baselines");
        }
    }
}
