using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public const string ClipPath = "Audio/beach_club_loop";

    AudioSource source;

    public AudioClip Clip => source.clip;
    public bool IsPlaying => source.isPlaying;

    public void Init()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>(ClipPath);
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0.45f;
    }

    public void SetPlaying(bool on)
    {
        if (on && !source.isPlaying) source.Play();
        else if (!on) source.Stop();
    }
}
