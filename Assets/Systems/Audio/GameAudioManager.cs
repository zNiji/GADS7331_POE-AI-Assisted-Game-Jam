using UnityEngine;
using UnityEngine.SceneManagement;

// Centralized gameplay audio for: music + shooting + impacts + pickups + mining + death + pause.
// If no AudioClips are assigned, this manager generates small procedural fallback clips
// so the game always has sound.
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Volumes")]
    [SerializeField] private float masterVolume = 1f;
    [SerializeField] private float musicVolume = 0.35f;
    [SerializeField] private float sfxVolume = 0.9f;

    [Header("Optional Music")]
    [SerializeField] private AudioClip levelMusicClip;

    // Optional SFX - if left null, procedural fallbacks are used.
    [Header("Optional SFX")]
    [SerializeField] private AudioClip sfxShootClip;
    [SerializeField] private AudioClip sfxEnemyShootClip;
    [SerializeField] private AudioClip sfxBulletHitClip;
    [SerializeField] private AudioClip sfxPickupClip;
    [SerializeField] private AudioClip sfxMineClip;
    [SerializeField] private AudioClip sfxHealClip;
    [SerializeField] private AudioClip sfxOxygenClip;
    [SerializeField] private AudioClip sfxAmmoPickupClip;
    [SerializeField] private AudioClip sfxPlayerDeathClip;
    [SerializeField] private AudioClip sfxPlayerHurtClip;
    [SerializeField] private AudioClip sfxEnemyDeathClip;
    [SerializeField] private AudioClip sfxExtractStartClip;
    [SerializeField] private AudioClip sfxExtractSuccessClip;
    [SerializeField] private AudioClip sfxExtractFailClip;
    [SerializeField] private AudioClip sfxPauseToggleClip;
    [SerializeField] private AudioClip sfxMenuClickClip;
    [Header("Optional Landing SFX")]
    [SerializeField] private AudioClip sfxPlayerLandClip;

    private AudioSource musicSource;

    // Cached procedural fallbacks.
    private AudioClip fallbackShoot;
    private AudioClip fallbackEnemyShoot;
    private AudioClip fallbackBulletHit;
    private AudioClip fallbackPickup;
    private AudioClip fallbackMine;
    private AudioClip fallbackHeal;
    private AudioClip fallbackOxygen;
    private AudioClip fallbackAmmoPickup;
    private AudioClip fallbackPlayerDeath;
    private AudioClip fallbackPlayerHurt;
    private AudioClip fallbackEnemyDeath;
    private AudioClip fallbackExtractStart;
    private AudioClip fallbackExtractSuccess;
    private AudioClip fallbackExtractFail;
    private AudioClip fallbackPauseToggle;
    private AudioClip fallbackMenuClick;
    private AudioClip fallbackPlayerLand;

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
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        ApplyMasterVolume();
        EnsureFallbackClips();
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // In case we were created mid-session.
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        try { SceneManager.sceneLoaded -= HandleSceneLoaded; } catch { }
        if (Instance == this) Instance = null;
    }

    private void ApplyMasterVolume()
    {
        AudioListener.volume = Mathf.Clamp01(masterVolume);
    }

    private void EnsureFallbackClips()
    {
        // Procedural “always works” fallbacks.
        if (sfxShootClip == null) fallbackShoot = GenerateToneClip("game_sfx_shoot", 0.08f, 900f, 0.35f, 1);
        if (sfxEnemyShootClip == null) fallbackEnemyShoot = GenerateToneClip("game_sfx_enemy_shoot", 0.10f, 520f, 0.28f, 1);
        if (sfxBulletHitClip == null) fallbackBulletHit = GenerateNoiseThumpClip("game_sfx_bullet_hit", 0.08f, 0.35f);
        if (sfxPickupClip == null) fallbackPickup = GenerateToneClip("game_sfx_pickup", 0.10f, 1180f, 0.25f, 1);
        if (sfxMineClip == null) fallbackMine = GenerateToneClip("game_sfx_mine", 0.10f, 300f, 0.20f, 1);
        if (sfxHealClip == null) fallbackHeal = GenerateToneClip("game_sfx_heal", 0.12f, 740f, 0.25f, 1);
        if (sfxOxygenClip == null) fallbackOxygen = GenerateToneClip("game_sfx_oxygen", 0.12f, 820f, 0.22f, 1);
        if (sfxAmmoPickupClip == null) fallbackAmmoPickup = GenerateToneClip("game_sfx_ammo_pickup", 0.10f, 680f, 0.25f, 1);
        if (sfxPlayerDeathClip == null) fallbackPlayerDeath = GenerateNoiseThumpClip("game_sfx_player_death", 0.35f, 0.50f);
        if (sfxPlayerHurtClip == null) fallbackPlayerHurt = GenerateToneClip("game_sfx_player_hurt", 0.08f, 160f, 0.22f, 1);
        if (sfxEnemyDeathClip == null) fallbackEnemyDeath = GenerateNoiseThumpClip("game_sfx_enemy_death", 0.25f, 0.45f);
        if (sfxExtractStartClip == null) fallbackExtractStart = GenerateToneClip("game_sfx_extract_start", 0.18f, 460f, 0.24f, 1);
        if (sfxExtractSuccessClip == null) fallbackExtractSuccess = GenerateToneClip("game_sfx_extract_success", 0.20f, 980f, 0.22f, 1);
        if (sfxExtractFailClip == null) fallbackExtractFail = GenerateNoiseThumpClip("game_sfx_extract_fail", 0.18f, 0.40f);
        if (sfxPauseToggleClip == null) fallbackPauseToggle = GenerateToneClip("game_sfx_pause", 0.08f, 320f, 0.28f, 1);
        if (sfxMenuClickClip == null) fallbackMenuClick = GenerateToneClip("game_sfx_menu_click", 0.05f, 1100f, 0.20f, 1);
        if (sfxPlayerLandClip == null) fallbackPlayerLand = GenerateNoiseThumpClip("game_sfx_player_land", 0.10f, 0.30f);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Start/stop music by scene.
        // MainMenu should be silent; Level_* scenes should play.
        bool isLevel = !string.IsNullOrWhiteSpace(scene.name) && scene.name.StartsWith("Level_");
        if (!isLevel)
        {
            StopMusic();
            return;
        }

        PlayMusic();
    }

    public void PlayMusic()
    {
        EnsureFallbackClips();
        if (musicSource == null) return;

        if (levelMusicClip == null)
        {
            // Use procedural fallback if no clip assigned.
            levelMusicClip = GenerateSimpleLoopMusic();
        }

        if (levelMusicClip == null) return;

        musicSource.clip = levelMusicClip;
        musicSource.volume = Mathf.Clamp01(musicVolume) * Mathf.Clamp01(masterVolume);

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }

    public void PlayMenuClick(Vector3? worldPos = null)
    {
        AudioClip clip = sfxMenuClickClip != null ? sfxMenuClickClip : fallbackMenuClick;
        PlaySfxAtPoint(clip, worldPos, 1f);
    }

    public void PlayPauseToggle()
    {
        AudioClip clip = sfxPauseToggleClip != null ? sfxPauseToggleClip : fallbackPauseToggle;
        PlaySfxAtPoint(clip, null, 1f);
    }

    public void PlayLand(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxPlayerLandClip != null ? sfxPlayerLandClip : fallbackPlayerLand;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayShoot(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxShootClip != null ? sfxShootClip : fallbackShoot;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayEnemyShoot(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxEnemyShootClip != null ? sfxEnemyShootClip : fallbackEnemyShoot;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayBulletHit(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxBulletHitClip != null ? sfxBulletHitClip : fallbackBulletHit;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayPickup(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxPickupClip != null ? sfxPickupClip : fallbackPickup;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayMine(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxMineClip != null ? sfxMineClip : fallbackMine;
        // Mining is frequent feedback; play non-spatially so it doesn't get attenuated.
        PlaySfxAtPoint(clip, null, volumeMultiplier);
    }

    public void PlayHeal(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxHealClip != null ? sfxHealClip : fallbackHeal;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayOxygen(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxOxygenClip != null ? sfxOxygenClip : fallbackOxygen;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayAmmoPickup(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxAmmoPickupClip != null ? sfxAmmoPickupClip : fallbackAmmoPickup;
        PlaySfxAtPoint(clip, position, volumeMultiplier);
    }

    public void PlayPlayerDeath(Vector3 position)
    {
        AudioClip clip = sfxPlayerDeathClip != null ? sfxPlayerDeathClip : fallbackPlayerDeath;
        PlaySfxAtPoint(clip, position, 1f);
    }

    public void PlayPlayerHurt(Vector3 position, float volumeMultiplier = 1f)
    {
        AudioClip clip = sfxPlayerHurtClip != null ? sfxPlayerHurtClip : fallbackPlayerHurt;
        // Hurt SFX must be clearly audible; play non-spatially to avoid attenuation issues.
        if (clip == null) return;
        float v = Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(volumeMultiplier);
        EnsureFallbackClips();
        if (musicSource != null)
        {
            musicSource.PlayOneShot(clip, v);
        }
    }

    public void PlayEnemyDeath(Vector3 position)
    {
        AudioClip clip = sfxEnemyDeathClip != null ? sfxEnemyDeathClip : fallbackEnemyDeath;
        PlaySfxAtPoint(clip, position, 1f);
    }

    public void PlayExtractStart(Vector3 position)
    {
        AudioClip clip = sfxExtractStartClip != null ? sfxExtractStartClip : fallbackExtractStart;
        PlaySfxAtPoint(clip, position, 1f);
    }

    public void PlayExtractSuccess(Vector3 position)
    {
        AudioClip clip = sfxExtractSuccessClip != null ? sfxExtractSuccessClip : fallbackExtractSuccess;
        PlaySfxAtPoint(clip, position, 1f);
    }

    public void PlayExtractFail(Vector3 position)
    {
        AudioClip clip = sfxExtractFailClip != null ? sfxExtractFailClip : fallbackExtractFail;
        PlaySfxAtPoint(clip, position, 1f);
    }

    private void PlaySfxAtPoint(AudioClip clip, Vector3? worldPos, float volumeMultiplier)
    {
        if (clip == null) return;

        float v = Mathf.Clamp01(sfxVolume) * Mathf.Clamp01(masterVolume) * Mathf.Clamp01(volumeMultiplier);
        Vector3 pos = worldPos.HasValue ? worldPos.Value : Vector3.zero;

        if (worldPos.HasValue)
        {
            AudioSource.PlayClipAtPoint(clip, pos, v);
        }
        else
        {
            // No world pos provided: just play non-spatially near listener.
            musicSource.PlayOneShot(clip, v);
        }
    }

    private AudioClip GenerateSimpleLoopMusic()
    {
        int sampleRate = 44100;
        float duration = 6f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        float[] scaleFreq =
        {
            220f,
            247f,
            262f,
            294f,
            330f,
            392f,
            440f
        };

        float beat = 0.20f;
        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            int step = Mathf.FloorToInt(t / beat);
            float localT = t - step * beat;

            float env = Mathf.Exp(-localT * 9f) * Mathf.Clamp01(1f - localT / beat);
            float f = scaleFreq[step % scaleFreq.Length];

            float s1 = Mathf.Sin(2f * Mathf.PI * f * t);
            float s2 = Mathf.Sin(2f * Mathf.PI * (f * 0.5f) * t);

            float s = (s1 * 0.55f + s2 * 0.22f) * env;
            data[i] = Mathf.Clamp(s, -1f, 1f) * 0.18f;
        }

        AudioClip clip = AudioClip.Create("game_music_generated", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateToneClip(string name, float durationSeconds, float baseFreq, float amplitude, int channels)
    {
        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 28f);
            float freq = baseFreq * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 2f * t));
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * amplitude;
        }

        AudioClip clip = AudioClip.Create(name, samples, channels, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateNoiseThumpClip(string name, float durationSeconds, float amplitude)
    {
        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 18f);
            float noise = Random.Range(-1f, 1f);
            float tone = Mathf.Sin(2f * Mathf.PI * (140f + 40f * Mathf.Sin(2f * Mathf.PI * 3f * t)) * t);
            data[i] = (noise * 0.55f + tone * 0.25f) * env * amplitude;
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}

