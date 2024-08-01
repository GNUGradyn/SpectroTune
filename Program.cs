// See https://aka.ms/new-console-template for more information

using CommandLine;

using CommandLine;

var interpretedParams = Parser.Default.ParseArguments<Options>(args);

var files = interpretedParams.Value.Files.Concat(
    interpretedParams.Value.Directories.SelectMany(x => Directory.GetFiles(x, "*.*", SearchOption.AllDirectories)));

if (!files.Any())
{
    Console.WriteLine("No files!");
    return;
}

class Options
{
    [Option('d', "directory", HelpText = "One or more directories to recursively fix")]
    public IEnumerable<string> Directories { get; set; }
    
    [Option('f', "file", HelpText = "One or more files to fix")]
    public IEnumerable<string> Files { get; set; }
    
    [Option("--ffmpeg", Default = "ffmpeg", HelpText = "Path to ffmpeg. Consults global PATH by default")]
    public string FfmpegPath { get; set; }
    
    [Option("--ffprobe", Default = "ffprobe", HelpText = "Path to ffprobe. Consults global PATH by default")]
    public string FfprobePath { get; set; }
    
    [Option('t', "threads", HelpText = "Sets the maximum degree of parallelism. Defaults to the number of threads on your system", Default = 0)] // Defaults to 0 since properties have to be compile-time constants. 0 will be interpreted as Environment.ProcessorCount
    public bool Threads { get; set; }
}