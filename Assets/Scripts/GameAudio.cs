using UnityEngine;
using UnityEngine.SceneManagement;

public enum SoundEffect { Jump, Coin, Bump, Break, Item, PowerUp, Damage, Stomp, Kick, Pipe, ExtraLife, Death, Complete, Swim }
public enum MusicTheme { Surface, Underground, Underwater, Menu }

/// <summary>Dos canales 2D persistentes; los clips propios se generan sin dependencias externas.</summary>
public sealed class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }
    private AudioSource music;
    private AudioSource effects;
    private AudioClip surfaceClip, undergroundClip, starClip, underwaterClip, menuClip;
    private AudioClip[] effectClips;
    private Player player;
    private MusicTheme theme;
    private bool suspended;

    private void Awake()
    {
        Instance = this;
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = effects.playOnAwake = false;
        music.spatialBlend = effects.spatialBlend = 0f;
        music.loop = true;
        music.volume = 0.25f;
        effects.volume = 0.6f;
        surfaceClip = Resources.Load<AudioClip>("RetroAudio/Surface");
        undergroundClip = Resources.Load<AudioClip>("RetroAudio/Underground");
        starClip = Resources.Load<AudioClip>("RetroAudio/Star");
        underwaterClip = Resources.Load<AudioClip>("RetroAudio/Underwater");
        menuClip = Resources.Load<AudioClip>("RetroAudio/Menu");
        effectClips = new AudioClip[System.Enum.GetValues(typeof(SoundEffect)).Length];
        for (int i = 0; i < effectClips.Length; i++)
            effectClips[i] = Resources.Load<AudioClip>("RetroAudio/" + (SoundEffect)i);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => player = FindFirstObjectByType<Player>();

    private void Update()
    {
        if (suspended) return;
        if (player == null && theme != MusicTheme.Menu) player = FindFirstObjectByType<Player>();
        AudioClip environment = theme == MusicTheme.Menu ? menuClip : theme == MusicTheme.Underwater ? underwaterClip : theme == MusicTheme.Underground ? undergroundClip : surfaceClip;
        AudioClip target = player != null && player.starpower && theme != MusicTheme.Menu ? starClip : environment;
        if (music.clip == target && music.isPlaying) return;
        music.clip = target;
        if (target != null) music.Play();
    }

    public void SetEnvironment(bool value) => SetTheme(value ? MusicTheme.Underground : MusicTheme.Surface);
    public void SetTheme(MusicTheme value) { theme = value; suspended = false; }
    public void StopMusic() { suspended = true; music.Stop(); }
    public void Play(SoundEffect effect)
    {
        AudioClip clip = effectClips[(int)effect];
        if (clip != null) effects.PlayOneShot(clip);
    }
    public void PlayerDied() { StopMusic(); Play(SoundEffect.Death); }
    public void LevelCompleted() { StopMusic(); Play(SoundEffect.Complete); }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}
