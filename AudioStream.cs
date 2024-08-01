namespace SpectroTune;

public class AudioStream
{
    public AudioStream(int index, double channelCount, string language)
    {
        ChannelCount = channelCount;
        Language = language;
        Index = index;
    }

    public double ChannelCount { get; set; }
    public string Language { get; set; }
    public int Index { get; set; }
}