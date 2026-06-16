using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using NLayer;

const float MinRms = 0.0005f;
const float MinScale = 0.25f;
const float MaxScale = 3f;
const string ReferenceGuid = "fd1652965664e954b89836adc362d526";
const string SfxFolder = "Assets/Arts/Audio/SFX";
const string CatalogPath = "Assets/Resources/Data/SfxCatalog.asset";

string projectRoot = args.Length > 0
	? Path.GetFullPath(args[0])
	: Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

string sfxRoot = Path.Combine(projectRoot, SfxFolder.Replace('/', Path.DirectorySeparatorChar));
string catalogFile = Path.Combine(projectRoot, CatalogPath.Replace('/', Path.DirectorySeparatorChar));

if (!Directory.Exists(sfxRoot) || !File.Exists(catalogFile))
{
	Console.Error.WriteLine("Project paths not found.");
	return 1;
}

var guidToFile = BuildGuidMap(sfxRoot);
string catalogText = File.ReadAllText(catalogFile, Encoding.UTF8);

var entryPattern = new Regex(
	@"(- id: (\d+)\s+clip: \{fileID: \d+, guid: ([a-f0-9]+), type: 3\}\s+volumeScale: )([0-9.]+)",
	RegexOptions.Multiline);

var matches = entryPattern.Matches(catalogText);
if (matches.Count == 0)
{
	Console.Error.WriteLine("No SfxCatalog entries found.");
	return 1;
}

if (!guidToFile.TryGetValue(ReferenceGuid, out string? referencePath) || referencePath == null)
{
	Console.Error.WriteLine("Reference clip (신의 방패) not found.");
	return 1;
}

float referenceRms = MeasureFileRms(referencePath);
float referenceVolumeScale = 1f;

foreach (Match match in matches)
{
	if (match.Groups[3].Value == ReferenceGuid)
	{
		referenceVolumeScale = float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
		break;
	}
}

if (referenceRms <= MinRms)
{
	Console.Error.WriteLine($"Reference RMS too low: {referenceRms}");
	return 1;
}

float targetEffective = referenceRms * referenceVolumeScale;
var scalesById = new Dictionary<int, float>();
var rmsById = new Dictionary<int, float>();
var namesById = new Dictionary<int, string>();
double rmsSum = 0d;
int rmsCount = 0;

foreach (Match match in matches)
{
	int id = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
	string guid = match.Groups[3].Value;

	if (!guidToFile.TryGetValue(guid, out string? filePath) || filePath == null)
	{
		Console.Error.WriteLine($"No audio file for guid {guid} (id {id})");
		return 1;
	}

	string baseName = Path.GetFileNameWithoutExtension(filePath);
	float clipRms = MeasureFileRms(filePath);
	namesById[id] = baseName;
	rmsById[id] = clipRms;

	if (clipRms > MinRms)
	{
		rmsSum += clipRms;
		rmsCount++;
	}

	float oldScale = float.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
	float currentEffective = clipRms * oldScale;
	float newScale = oldScale;

	if (clipRms > MinRms && currentEffective > targetEffective)
		newScale = MathfClamp(targetEffective / clipRms, MinScale, MaxScale);

	scalesById[id] = newScale;
}

string updated = entryPattern.Replace(catalogText, m =>
{
	int id = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
	float scale = scalesById[id];
	return $"{m.Groups[1].Value}{scale.ToString("G9", CultureInfo.InvariantCulture)}";
});

File.WriteAllText(catalogFile, updated, Encoding.UTF8);

Console.WriteLine("SFX volume balance — attenuate only (reference: 신의 방패)");
Console.WriteLine($"  clip RMS          = {referenceRms:F6}");
Console.WriteLine($"  volumeScale       = {referenceVolumeScale:F4} (unchanged)");
Console.WriteLine($"  target factor     = {targetEffective:F6} (rms x scale)");
Console.WriteLine($"  all clips avg RMS = {(rmsCount > 0 ? rmsSum / rmsCount : 0d):F6}");
Console.WriteLine($"Updated {scalesById.Count} catalog entries.\n");

foreach (int id in scalesById.Keys.OrderBy(k => k))
{
	float oldScale = float.Parse(
		matches.Cast<Match>().First(m => int.Parse(m.Groups[2].Value) == id).Groups[4].Value,
		CultureInfo.InvariantCulture);
	Console.WriteLine(
		$"{id,2} {namesById[id],-30} rms={rmsById[id]:F5}  {oldScale:F4} -> {scalesById[id]:F4}");
}

return 0;

static Dictionary<string, string> BuildGuidMap(string sfxRoot)
{
	var map = new Dictionary<string, string>();
	foreach (string metaPath in Directory.GetFiles(sfxRoot, "*.meta", SearchOption.AllDirectories))
	{
		if (metaPath.Contains($"{Path.DirectorySeparatorChar}_Misplaced{Path.DirectorySeparatorChar}"))
			continue;

		string text = File.ReadAllText(metaPath);
		var guidMatch = Regex.Match(text, @"^guid:\s*([a-f0-9]+)\s*$", RegexOptions.Multiline);
		if (!guidMatch.Success)
			continue;

		string audioPath = metaPath[..^5];
		if (File.Exists(audioPath))
			map[guidMatch.Groups[1].Value] = audioPath;
	}

	return map;
}

static float MeasureFileRms(string path)
{
	string ext = Path.GetExtension(path).ToLowerInvariant();
	float[] samples = ext switch
	{
		".wav" => ReadWavSamples(path),
		".mp3" => ReadMp3Samples(path),
		".ogg" => throw new NotSupportedException($"OGG not supported offline: {path}"),
		_ => throw new NotSupportedException($"Unsupported audio: {path}"),
	};

	if (samples.Length == 0)
		return 0f;

	const int maxSamplesToScan = 44100 * 120 * 2;
	int stride = Math.Max(1, samples.Length / maxSamplesToScan);
	double sumSquares = 0d;
	int count = 0;
	for (int i = 0; i < samples.Length; i += stride)
	{
		float sample = samples[i];
		sumSquares += sample * sample;
		count++;
	}

	return count > 0 ? (float)Math.Sqrt(sumSquares / count) : 0f;
}

static float[] ReadWavSamples(string path)
{
	using var stream = File.OpenRead(path);
	using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

	if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
		throw new InvalidDataException($"Not RIFF: {path}");

	reader.ReadInt32();
	if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
		throw new InvalidDataException($"Not WAVE: {path}");

	short audioFormat = 1;
	short channels = 1;
	short bitsPerSample = 16;
	byte[]? data = null;

	while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
	{
		string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
		int chunkSize = reader.ReadInt32();

		if (chunkId == "fmt ")
		{
			audioFormat = reader.ReadInt16();
			channels = reader.ReadInt16();
			reader.ReadInt32(); // sample rate
			reader.ReadInt32(); // byte rate
			reader.ReadInt16(); // block align
			bitsPerSample = reader.ReadInt16();

			int readFmt = 16;
			if (chunkSize > readFmt)
			{
				int extra = chunkSize - readFmt;
				byte[] extraBytes = reader.ReadBytes(extra);
				if ((audioFormat == -2 || audioFormat == unchecked((short)0xFFFE)) && extraBytes.Length >= 22)
				{
					short subFormat = BitConverter.ToInt16(extraBytes, 8);
					audioFormat = subFormat;
				}
			}
		}
		else if (chunkId == "data")
		{
			data = reader.ReadBytes(chunkSize);
			break;
		}
		else
		{
			reader.ReadBytes(chunkSize);
		}
	}

	if (data == null || data.Length == 0)
		return Array.Empty<float>();

	if (audioFormat != 1 && audioFormat != 3)
		throw new NotSupportedException($"WAV format {audioFormat} not supported: {path}");

	int bytesPerSample = Math.Max(1, (int)bitsPerSample / 8);
	int frameSize = bytesPerSample * Math.Max(1, (int)channels);
	var samples = new List<float>(data.Length / frameSize);

	for (int i = 0; i + frameSize <= data.Length; i += frameSize)
	{
		double mixed = 0d;
		for (int ch = 0; ch < channels; ch++)
		{
			int offset = i + ch * bytesPerSample;
			float sample = audioFormat switch
			{
				3 when bitsPerSample == 32 => BitConverter.ToSingle(data, offset),
				3 when bitsPerSample == 64 => (float)BitConverter.ToDouble(data, offset),
				_ when bitsPerSample == 16 => (short)(data[offset] | (data[offset + 1] << 8)) / 32768f,
				_ when bitsPerSample == 24 =>
					SignExtend24(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16)) / 8388608f,
				_ when bitsPerSample == 32 => BitConverter.ToInt32(data, offset) / 2147483648f,
				_ => throw new NotSupportedException($"Bit depth {bitsPerSample}: {path}"),
			};

			mixed += sample;
		}

		samples.Add((float)(mixed / Math.Max(1, (int)channels)));
	}

	return samples.ToArray();
}

static float[] ReadMp3Samples(string path)
{
	using var file = File.OpenRead(path);
	var mpeg = new MpegFile(file);
	int channels = Math.Max(1, mpeg.Channels);
	var buffer = new float[channels * 4096];
	var samples = new List<float>();

	while (true)
	{
		int read = mpeg.ReadSamples(buffer, 0, buffer.Length);
		if (read <= 0)
			break;

		if (channels >= 2)
		{
			for (int i = 0; i + 1 < read; i += channels)
				samples.Add((buffer[i] + buffer[i + 1]) * 0.5f);
		}
		else
		{
			for (int i = 0; i < read; i++)
				samples.Add(buffer[i]);
		}
	}

	return samples.ToArray();
}

static float MathfClamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));

static int SignExtend24(int value)
{
	return (value & 0x800000) != 0 ? value | unchecked((int)0xFF000000) : value;
}
