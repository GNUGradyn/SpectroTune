// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using Spectre.Console;
using Spectre.Console.Rendering;
using SpectroTune;

var interpretedParams = Parser.Default.ParseArguments<Options>(args);

var ffmpegLocation = string.IsNullOrWhiteSpace(interpretedParams.Value.FfmpegPath)
    ? LocateExecutable("ffmpeg")
    : interpretedParams.Value.FfmpegPath;

if (string.IsNullOrWhiteSpace(ffmpegLocation) || !File.Exists(ffmpegLocation))
{
    Console.WriteLine("Failed to locate FFMPEG: No path provided and ffmpeg is not in your path, or the provided path does not exist");
    return;
}

var ffprobeLocation = string.IsNullOrWhiteSpace(interpretedParams.Value.FfprobePath)
    ? LocateExecutable("ffprobe")
    : interpretedParams.Value.FfprobePath;

if (string.IsNullOrWhiteSpace(ffprobeLocation) || !File.Exists(ffprobeLocation))
{
    Console.WriteLine("Failed to locate FFMPEG: No path provided and ffmpeg is not in your path, or the provided path does not exist");
    return;
}

Console.WriteLine("Collecting files, please wait");

var files = interpretedParams.Value.Files.Concat(
    interpretedParams.Value.Directories.SelectMany(x => Directory.GetFiles(x, "*.*", SearchOption.AllDirectories))).ToList();

if (!files.Any())
{
    Console.WriteLine("No files!");
    return;
}

var maxDegreeParallelism = interpretedParams.Value.Threads == 0 ? Environment.ProcessorCount : interpretedParams.Value.Threads;

Console.WriteLine($"Processing {files.Count} files on {maxDegreeParallelism} threads");

var remaining = files.Count;

var table = new Table();
table.AddColumn("File");
table.AddColumn("Status");
table.AddColumn("Stream Index");
table.AddColumn("Progress");

// This is due to a VERY annoying limitation in Spectre.Console that they have yet to fix.
// We cannot find the index of a TableRow in table.Rows. 
// We can't do it by extending IReadOnlyList (and therefore TableRowCollection) because TableRow does not implement iComparable nor does it maintain its reference
// We also cannot extend TableRow to hold an identifier or be otherwise comparable because it is sealed.
// Therfore we have to maintain a second list that is only used to keep track of the indexes in the table.
// We lock both collections with 1 lock to avoid race conditions
var workerListLock = new object();
var workerPool = new List<string>();

AnsiConsole.Live(table).Start(consoleCtx =>
{
    Parallel.ForEach(files, new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeParallelism }, file =>
    {
        var row = new TableRow(new Markup[] { new(Markup.Escape(Path.GetFileName(file))), new("Initial File Analysis"), new("N/A"), new("0%") });
        lock (workerListLock)
        {
            table.Rows.Add(row);
            workerPool.Add(file);
        }
        consoleCtx.Refresh();
        var streams = GetAudioStreams(file);
        foreach (var audioStream in streams)
        {
            table.UpdateCell(workerPool.IndexOf(file), 1, "Stream Analysis");
            table.UpdateCell(workerPool.IndexOf(file), 2, audioStream.Index.ToString());
            consoleCtx.Refresh();
            var initialLevel = GetDecibelPeakOfStream(file, audioStream.Index, audioStream.Duration, progress => 
            {
                table.UpdateCell(workerPool.IndexOf(file), 3, (int)(progress * 100) + "%");
                consoleCtx.Refresh();
            });
        }
        lock (workerListLock)
        {
            var index = workerPool.IndexOf(file);
            table.Rows.RemoveAt(index);
            workerPool.RemoveAt(index);
        }
        consoleCtx.Refresh();
        remaining--;
    });
});

double GetDecibelPeakOfStream(string file, int streamIndex, TimeSpan duration, Action<double>? progress = null)
{ 
    var result = ExecuteFfmpeg([
        "-i", $"\"{file}\"", $"-filter:a:{streamIndex.ToString()}", "volumedetect", "-f", "null", "/dev/null", "-threads", (remaining >= maxDegreeParallelism ? 1 : maxDegreeParallelism - files.Count).ToString()
    ], null, (sender, eventArgs) =>
    {
        if (progress == null) return;
        if (eventArgs.Data == null) return;
        if (eventArgs.Data.Contains("time="))
        {
            progress(TimeSpan.Parse(Regex.Match(eventArgs.Data, @"time=(.*?)(?=\.)").Groups[1].Value).TotalSeconds /
                     duration.TotalSeconds);
        }
    });
    return 0;
}

AudioStream[] GetAudioStreams(string filePath)
{
    var rawResult = ExecuteFfprobe(
        ["-v", "error", "-select_streams", "a", "-show_entries", "stream=index,channels:stream_tags=language,duration", "-of", "csv=p=0", $"\"{filePath}\""]);
    return rawResult
        .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x =>
        {
            if (x.Split(",").Length == 3)
            {
                return new AudioStream(int.Parse(x.Split(',')[0]) - 1, double.Parse(x.Split(',')[1]), "unk", TimeSpan.Parse(x.Split(',')[2].Split(".")[0]));

            }
            return new AudioStream(int.Parse(x.Split(',')[0]) - 1, double.Parse(x.Split(',')[1]), x.Split(',')[2], TimeSpan.Parse(x.Split(',')[3].Split(".")[0]));
        })
        .ToArray();
}


string ExecuteFfmpeg(string[] cmdArgs, DataReceivedEventHandler? dataReceived = null, DataReceivedEventHandler? errorReceived = null)
{
    return ProcessUtils.ExecuteCommand(ffmpegLocation, cmdArgs, dataReceived, errorReceived);
}

string ExecuteFfprobe(string[] cmdArgs, DataReceivedEventHandler? dataReceived = null, DataReceivedEventHandler? errorReceived = null)
{
    return ProcessUtils.ExecuteCommand(ffprobeLocation, cmdArgs, dataReceived, errorReceived);
}

string? LocateExecutable(string filename)
{
    return Environment.GetEnvironmentVariable("PATH")?.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).SelectMany(Directory.GetFiles)
        .FirstOrDefault(x => string.Equals(Path.GetFileName(x).Replace(".exe", ""), filename, StringComparison.CurrentCultureIgnoreCase));
}
