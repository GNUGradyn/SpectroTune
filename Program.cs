// See https://aka.ms/new-console-template for more information

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

string ExecuteCommand(string path, string[] cmdArgs)
{
    
}