namespace SpectroTune;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Worker : INotifyPropertyChanged
{
    private string _file;
    private int _currentStream;
    private AudioStream[] _streams;
    private State _state;

    public Worker(string file, int currentStream, AudioStream[] streams, State state)
    {
        _file = file;
        _currentStream = currentStream;
        _streams = streams;
        _state = state;
    }

    public string File
    {
        get => _file;
        set
        {
            if (_file != value)
            {
                _file = value;
                OnPropertyChanged();
            }
        }
    }

    public int CurrentStream
    {
        get => _currentStream;
        set
        {
            if (_currentStream != value)
            {
                _currentStream = value;
                OnPropertyChanged();
            }
        }
    }

    public AudioStream[] Streams
    {
        get => _streams;
        set
        {
            if (_streams != value)
            {
                _streams = value;
                OnPropertyChanged();
            }
        }
    }

    public State State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


public enum State
{
    StreamExport,
    StreamAnalysis,
    StreamConversion
}