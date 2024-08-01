// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using CommandLine;
using SpectroTune;

var interpretedParams = Parser.Default.ParseArguments<Options>(args);

var files = interpretedParams.Value.Files.Concat(
    interpretedParams.Value.Directories.SelectMany(x => Directory.GetFiles(x, "*.*", SearchOption.AllDirectories)));

if (!files.Any())
{
    Console.WriteLine("No files!");
    return;
}

var maxDegreeParallelism = interpretedParams.Value.Threads == 0 ? Environment.ProcessorCount : interpretedParams.Value.Threads;

Console.WriteLine($"Processing {files.Count()} files on {maxDegreeParallelism} threads");


Parallel.ForEach(files, new ParallelOptions() {MaxDegreeOfParallelism = maxDegreeParallelism}, file =>
{
    
});

int[] GetAudioStreams(string filePath)
{
    var rawResult = ExecuteCommand(interpretedParams.Value.FfprobePath,
        ["-v", "error", "-select_streams", "a", "-show_entries", "stream=index,channels:stream_tags=language", "-of", "csv=p=0", $"\"{filePath}\""]);
}

string ExecuteCommand(string path, string[] cmdArgs)
{
    Process p = new Process();
    p.StartInfo.UseShellExecute = false;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.FileName = path;
    p.StartInfo.Arguments = string.Join(' ', cmdArgs);
    p.StartInfo.CreateNoWindow = true;
    p.Start();
    string output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return output;
}