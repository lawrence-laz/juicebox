namespace JuiceboxEngine;

public class AudioPlayer
{
    private readonly SdlFacade _sdl;
    private readonly SoundRepository _soundRepository;
    private readonly SoundFactory _soundFactory;
    private readonly MusicRepository _musicRepository;
    private readonly MusicFactory _musicFactory;

    public AudioPlayer(
        SdlFacade sdl,
        SoundRepository soundRepository,
        SoundFactory soundFactory,
        MusicRepository musicRepository,
        MusicFactory musicFactory)
    {
        _sdl = sdl;
        _soundRepository = soundRepository;
        _soundFactory = soundFactory;
        _musicRepository = musicRepository;
        _musicFactory = musicFactory;
    }

    public void Play(Sound sound) => _sdl.PlaySound(sound);
    public void PlaySound(string soundPath)
    {
        if (_soundRepository.GetByPath(soundPath) is not Sound sound)
        {
            sound = _soundFactory.Create(soundPath);
        }
        Play(sound);
    }
    public void Play(Music music) => _sdl.PlayMusic(music);
    public void PlayMusic(string musicPath)
    {
        if (_musicRepository.GetByPath(musicPath) is not Music music)
        {
            music = _musicFactory.Create(musicPath);
        }
        _sdl.PlayMusic(music);
    }
    public void PauseMusic() => _sdl.PauseMusic();
    public void ResumeMusic() => _sdl.ResumeMusic();
}

public class MusicFactory
{
    private readonly SdlFacade _sdl;
    private readonly MusicRepository _musicRepository;

    public MusicFactory(SdlFacade sdl, MusicRepository musicRepository)
    {
        _sdl = sdl;
        _musicRepository = musicRepository;
    }

    public Music Create(string path)
    {
        var music = new Music(path);
        _sdl.LoadMusic(music);
        _musicRepository.Add(music);
        return music;
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

public class Music
{
    public string Path { get; }

    public Music(string path)
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

