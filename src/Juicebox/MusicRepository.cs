namespace JuiceboxEngine;

public class MusicRepository
{
    public Dictionary<string, Music> _musics = new();

    public void Add(Music music)
    {
        if (_musics.ContainsKey(music.Path))
        {
            throw new Exception($"Music '{music.Path}' already exists.");
        }
        _musics[music.Path] = music;
    }

    public Music? GetByPath(string path) => _musics.GetValueOrDefault(path);
}
