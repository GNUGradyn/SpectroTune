namespace SpectroTune;

public class FFprobeResult
{
    public List<FFprobeStream> Streams { get; set; }
    public FFprobeFormat Format { get; set; }
    
    public class FFprobeStream
    {
        public int Index { get; set; }
        public int Channels { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public double? Duration { get; set; }
    }

    public class FFprobeFormat
    {
        public double Duration { get; set; }
    }
}