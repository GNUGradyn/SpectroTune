namespace SpectroTune;

public class ConsoleOutput
{
    public ConsoleOutput(string output, string error)
    {
        Output = output;
        Error = error;
    }

    public string Output { get; set; }
    public string Error { get; set; }
}