namespace JuiceboxEngine;

public class AudioPlayer
{
    private readonly SdlFacade _sdl;
    private readonly SoundRepository _soundRepository;
    private readonly SoundFactory _soundFactory;

    public AudioPlayer(SdlFacade sdl, SoundRepository soundRepository, SoundFactory soundFactory)
    {
        _sdl = sdl;
        _soundRepository = soundRepository;
        _soundFactory = soundFactory;
    }

    public void Play(Sound sound) => _sdl.PlaySound(sound);
    public void Play(string soundPath)
    {
        if (_soundRepository.GetByPath(soundPath) is not Sound sound)
        {
            sound = _soundFactory.Create(soundPath);
        }
        Play(sound);
    }
}

public class Sound
{
    public string Path { get; }

    public Sound(string path)
    {
        Path = path;
    }
}

public class SoundRepository
{
    private readonly Dictionary<string, Sound> _sounds = new();

    public void Add(Sound sound)
    {
        if (_sounds.ContainsKey(sound.Path))
        {
            throw new ArgumentException($"Sound '{sound.Path}' already exists.", nameof(sound));
        }
        _sounds[sound.Path] = sound;
    }

    internal Sound? GetByPath(string soundPath)
    {
        return _sounds.GetValueOrDefault(soundPath);
    }
}

public class SoundFactory
{
    private readonly SdlFacade _sdl;
    private readonly SoundRepository _soundRepository;

    public SoundFactory(SdlFacade sdl, SoundRepository soundRepository)
    {
        _sdl = sdl;
        _soundRepository = soundRepository;
    }

    public Sound Create(string path)
    {
        var sound = new Sound(path);
        _sdl.LoadSound(sound);
        _soundRepository.Add(sound);

        return sound;
    }
}

