using UnityEngine;
using UnityEngine.UI;

public class MainMenuAudioManager : MonoBehaviour
{
    public static MainMenuAudioManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip menuClickSound;
    [SerializeField] private AudioClip menuBackgroundMusic;
    [SerializeField] private float clickVolume = 1f;
    [SerializeField] private float musicVolume = 0.6f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        EnsureGeneratedClipsIfMissing();

        if (menuBackgroundMusic != null)
        {
            musicSource.clip = menuBackgroundMusic;
            musicSource.volume = Mathf.Clamp01(musicVolume);
            musicSource.Play();
        }
    }

    private void EnsureGeneratedClipsIfMissing()
    {
        if (menuClickSound == null)
        {
            menuClickSound = GenerateClickBeep();
        }

        if (menuBackgroundMusic == null)
        {
            menuBackgroundMusic = GenerateSimpleLoopMusic();
        }
    }

    private AudioClip GenerateClickBeep()
    {
        int sampleRate = 44100;
        float duration = 0.06f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        float freq = 880f; // A5
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 35f); // fast decay
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
        }

        AudioClip clip = AudioClip.Create("menu_click_generated", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateSimpleLoopMusic()
    {
        int sampleRate = 44100;
        float duration = 4f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        // A tiny "chiptune-ish" arpeggio loop.
        float[] scaleFreq =
        {
            220f, // A3
            247f, // B3-ish
            262f, // C4
            294f, // D4
            330f, // E4
            392f  // G4-ish
        };

        float beat = 0.25f; // 16th-ish steps
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;

            int step = Mathf.FloorToInt(t / beat);
            float localT = t - step * beat;

            // Envelope per step.
            float env = Mathf.Exp(-localT * 10f) * Mathf.Clamp01(1f - localT / beat);

            float f1 = scaleFreq[step % scaleFreq.Length];
            float f2 = scaleFreq[(step + 2) % scaleFreq.Length];

            float wave1 = Mathf.Sin(2f * Mathf.PI * f1 * t);
            float wave2 = Mathf.Sin(2f * Mathf.PI * (f2 * 0.5f) * t);

            // Add a quiet buzz so it feels "alive".
            float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * (f1 * 2.0f) * t)) * 0.08f;

            float s = (wave1 * 0.5f + wave2 * 0.25f) * env + buzz * env;
            data[i] = Mathf.Clamp(s, -1f, 1f) * 0.25f;
        }

        AudioClip clip = AudioClip.Create("menu_music_generated", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayClick()
    {
        if (menuClickSound == null) return;

        sfxSource.PlayOneShot(menuClickSound, Mathf.Clamp01(clickVolume));
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    public void PlayMusic()
    {
        EnsureGeneratedClipsIfMissing();

        if (musicSource == null) return;
        if (menuBackgroundMusic == null) return;

        // Restart if it was stopped by gameplay.
        musicSource.clip = menuBackgroundMusic;
        musicSource.volume = Mathf.Clamp01(musicVolume);

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void AttachClickSound(Button button)
    {
        if (button == null) return;

        // Marker component so we don't stack listeners.
        if (button.GetComponent<MenuButtonClickMarker>() != null) return;

        button.gameObject.AddComponent<MenuButtonClickMarker>();
        button.onClick.AddListener(PlayClick);
    }

    private class MenuButtonClickMarker : MonoBehaviour { }
}

