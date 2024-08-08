namespace SpectroTune;

public class AudioStream
{
    public AudioStream(int index, double channelCount, string language, TimeSpan duration)
    {
        ChannelCount = channelCount;
        Language = language;
        Index = index;
        Duration = duration;
    }

    public double ChannelCount { get; set; }
    public string Language { get; set; }
    public int Index { get; set; }
    public TimeSpan Duration { get; set; }
}