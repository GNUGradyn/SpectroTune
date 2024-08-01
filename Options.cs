using CommandLine;

namespace SpectroTune;

class Options
{
    [Option('d', "directory", HelpText = "One or more directories to recursively fix")]
    public IEnumerable<string> Directories { get; set; }
    
    [Option('f', "file", HelpText = "One or more files to fix")]
    public IEnumerable<string> Files { get; set; }
    
    [Option("ffmpeg", Default = "", HelpText = "Path to ffmpeg. Consults global PATH by default")] // Default is blank because properties have to be compile-time constants. PATH will be consulted at runtime to populate this if empty
    public string FfmpegPath { get; set; }
    
    [Option("ffprobe", Default = "", HelpText = "Path to ffprobe. Consults global PATH by default")] // Default is blank because properties have to be compile-time constants. PATH will be consulted at runtime to populate this if empty
    public string FfprobePath { get; set; }
    
    [Option('t', "threads", HelpText = "Sets the maximum degree of parallelism. Defaults to the number of threads on your system", Default = 0)] // Defaults to 0 since properties have to be compile-time constants. 0 will be interpreted as Environment.ProcessorCount
    public int Threads { get; set; }
    
    [Option("db", HelpText = "Sets the target level in Decibels. Increase to increase volume, decrease to decrease clipping. This will default to -1 which is a good target in most use cases, so you probably won't need to mess with this", Default = -1)]
    public float decibelTarget { get; set; }
    
    [Option("--stereo", HelpText = "Adds a volume-corrected downmixed stereo channel to surround-only files. This is disabled by default as you would not intuitively expect the script to do this, but enabling it is reccomended", Default = false)]
    public bool stereo { get; set; }
}