﻿// See https://aka.ms/new-console-template for more information

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using CommandLine;
using Spectre.Console;
using SpectroTune;

var interpretedParams = Parser.Default.ParseArguments<Options>(args);

var ffmpegLocation = string.IsNullOrWhiteSpace(interpretedParams.Value.FfmpegPath)
    ? LocateExecutable("ffmpeg")
    : interpretedParams.Value.FfmpegPath;

if (ffmpegLocation == null || !File.Exists(ffmpegLocation))
{
    Console.WriteLine("Failed to locate FFMPEG: No path provided and ffmpeg is not in your path, or the provided path does not exist");
}

var ffprobeLocation = string.IsNullOrWhiteSpace(interpretedParams.Value.FfprobePath)
    ? LocateExecutable("ffprobe")
    : interpretedParams.Value.FfprobePath;

if (ffprobeLocation == null || !File.Exists(ffprobeLocation))
{
    Console.WriteLine("Failed to locate FFMPEG: No path provided and ffmpeg is not in your path, or the provided path does not exist");
}

Console.WriteLine("Collecting files, please wait");

var files = interpretedParams.Value.Files.Concat(
    interpretedParams.Value.Directories.SelectMany(x => Directory.GetFiles(x, "*.*", SearchOption.AllDirectories)));

if (!files.Any())
{
    Console.WriteLine("No files!");
    return;
}

var maxDegreeParallelism = interpretedParams.Value.Threads == 0 ? Environment.ProcessorCount : interpretedParams.Value.Threads;

Console.WriteLine($"Processing {files.Count()} files on {maxDegreeParallelism} threads");

ObservableCollection<Worker> workers = [];
workers.CollectionChanged += WorkerCollectionUpdated;

void WorkerCollectionUpdated(object sender, NotifyCollectionChangedEventArgs args)
{
    RenderConsole();
}

void RenderConsole()
{
    var table = new Table();
    table.AddColumn("File");
    table.AddColumn("Status");
    foreach (var worker in workers)
    {
        table.AddRow(Path.GetFileName(worker.File), worker.State.ToString());
    }
    AnsiConsole.Write(table);
}

Parallel.ForEach(files, new ParallelOptions() {MaxDegreeOfParallelism = maxDegreeParallelism}, file =>
{
    var worker = new Worker(file, 0, [], State.StreamAnalysis);
    worker.PropertyChanged += (_, _) => RenderConsole();
    workers.Add(worker);
    //var streams = GetAudioStreams(file);
    Thread.Sleep(100);
    worker.State = State.StreamConversion;
    Thread.Sleep(100);
    workers.Remove(worker);
});

AudioStream[] GetAudioStreams(string filePath)
{
    var rawResult = ExecuteCommand(interpretedParams.Value.FfprobePath,
        ["-v", "error", "-select_streams", "a", "-show_entries", "stream=index,channels:stream_tags=language", "-of", "csv=p=0", $"\"{filePath}\""]);
    return rawResult
        .Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
        .Select(x => new AudioStream(int.Parse(x.Split(',')[0]), double.Parse(x.Split(',')[1]), x.Split(',')[2]))
        .ToArray();
}

string ExecuteCommand(string path, string[] cmdArgs)
{
    Process p = new Process();
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.RedirectStandardError = true;
    p.StartInfo.FileName = path;
    p.StartInfo.Arguments = string.Join(' ', cmdArgs);
    p.StartInfo.CreateNoWindow = true;
    p.Start();
    string output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    if (p.ExitCode != 0)
    {
        throw new Exception(p.StandardError.ReadToEnd());
    }
    return output;
}

string ExecuteFfmpeg(string[] cmdArgs)
{
    return ExecuteCommand(ffmpegLocation, cmdArgs);
}

string ExecuteFfprobe(string[] cmdArgs)
{
    return ExecuteCommand(ffprobeLocation, cmdArgs);
}

string? LocateExecutable(string filename)
{
    return Environment.GetEnvironmentVariable("PATH")?.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).SelectMany(Directory.GetFiles)
        .Select(x => x.Replace(".exe", "")).FirstOrDefault(x => x == filename);
}