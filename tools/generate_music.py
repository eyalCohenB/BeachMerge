"""Synthesizes a seamless 16-bar tropical house loop for the game's background music."""
import os
import wave
import numpy as np

SR = 44100
SPB = 22810  # samples per beat, ~116 BPM
BEATS = 64   # 16 bars of 4/4
N = SPB * BEATS
rng = np.random.default_rng(7)

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Audio", "beach_club_loop.wav")


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


def place(track, sound, start):
    """Adds a sound at a sample offset, wrapping past the end so the loop stays seamless."""
    idx = (np.arange(len(sound)) + start) % N
    np.add.at(track, idx, sound)


def lowpass(x, cutoff, order=2):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    return np.fft.irfft(spec / (1 + (f / cutoff) ** 2) ** (order / 2), len(x))


def highpass(x, cutoff, order=2):
    spec = np.fft.rfft(x)
    f = np.fft.rfftfreq(len(x), 1 / SR)
    resp = (f / cutoff) ** order / (1 + (f / cutoff) ** 2) ** (order / 2)
    return np.fft.irfft(spec * resp, len(x))


def t_of(seconds):
    return np.arange(int(seconds * SR)) / SR


# --- drums -------------------------------------------------------------------
def kick():
    t = t_of(0.35)
    freq = 48 + 90 * np.exp(-t * 32)
    phase = 2 * np.pi * np.cumsum(freq) / SR
    return np.sin(phase) * np.exp(-t * 8.5) * 0.95


def clap():
    t = t_of(0.18)
    burst = rng.standard_normal(len(t)) * (np.exp(-t * 28) + 0.6 * np.exp(-((t - 0.012) * 400) ** 2))
    return burst * 0.22


def hat(length, decay, gain):
    t = t_of(length)
    return rng.standard_normal(len(t)) * np.exp(-t * decay) * gain


# --- harmony -----------------------------------------------------------------
CHORDS = [  # A minor -> F -> C -> G, two bars each
    (45, [57, 60, 64]),
    (41, [57, 60, 65]),
    (48, [55, 60, 64]),
    (43, [55, 59, 62]),
]


def chord_at(beat):
    return CHORDS[(beat // 8) % 4]


def bass_note(midi, length):
    t = t_of(length)
    f = hz(midi)
    env = np.minimum(t / 0.004, 1) * np.exp(-t * 7)
    return (np.sin(2 * np.pi * f * t) + 0.35 * np.sin(4 * np.pi * f * t)) * env * 0.42


def pad_chord(notes, length):
    t = t_of(length)
    out = np.zeros(len(t))
    for m in notes:
        for detune in (-0.07, 0.0, 0.07):
            f = hz(m + detune)
            for h in range(1, 7):
                out += np.sin(2 * np.pi * f * h * t + h) / h
    attack = np.minimum(t / 0.08, 1)
    release = np.minimum((length - t) / 0.15, 1)
    return out * attack * release * 0.018


def marimba(midi, length=0.6):
    t = t_of(length)
    f = hz(midi)
    tone = np.sin(2 * np.pi * f * t) + 0.25 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t * 25)
    return tone * np.exp(-t * 7) * np.minimum(t / 0.002, 1) * 0.2


# Lead riff in 16th-note steps per 2-bar phrase (None = rest). A minor pentatonic.
RIFF_A = [69, None, 72, None, 76, None, 74, 72, None, 69, None, 72, None, 74, None, None,
          76, None, 79, None, 76, None, 74, 72, None, 74, None, 72, 69, None, None, None]
RIFF_B = [81, None, 79, 76, None, 79, None, 76, 74, None, 72, None, 74, 76, None, None,
          72, None, 74, None, 76, None, 79, None, 81, None, 79, 76, None, 74, 72, None]


def render():
    drums = np.zeros(N)
    bass = np.zeros(N)
    pad = np.zeros(N)
    lead = np.zeros(N)
    shaker = np.zeros(N)

    k, c = kick(), clap()
    for beat in range(BEATS):
        start = beat * SPB
        place(drums, k, start)
        if beat % 2 == 1:
            place(drums, c, start)
        place(drums, hat(0.07, 70, 0.10), start + SPB // 2)
        for s in range(4):
            place(shaker, hat(0.04, 110, 0.03 * (0.6 + 0.4 * rng.random())), start + s * SPB // 4)

        root, notes = chord_at(beat)
        place(bass, bass_note(root, SPB / 2 / SR), start + SPB // 2)
        if beat % 8 == 0:
            place(pad, pad_chord(notes, SPB * 8 / SR), start)

    step = SPB // 4
    for phrase in range(8):
        riff = RIFF_A if phrase < 4 or phrase % 2 == 0 else RIFF_B
        if phrase < 2:
            continue  # let the groove breathe before the lead comes in
        for i, note in enumerate(riff):
            if note is not None:
                place(lead, marimba(note), phrase * 8 * SPB + i * step)

    # Sidechain pump: duck pad and bass right after every kick.
    t_beat = (np.arange(N) % SPB) / SR
    duck = 1 - 0.65 * np.exp(-t_beat * 9)

    drums = lowpass(highpass(drums, 30), 7000)
    shaker = lowpass(highpass(shaker, 6000), 11000)
    pad = lowpass(pad, 1800) * duck
    bass = lowpass(bass, 900) * duck
    lead = lowpass(lead, 5000)
    echo = np.roll(lead, int(SPB * 0.75)) * 0.35 + np.roll(lead, int(SPB * 1.5)) * 0.15

    left = drums + bass + pad + lead + 0.8 * echo + shaker * 1.2
    right = drums + bass + np.roll(pad, 441) + 0.8 * lead + echo + shaker * 0.8

    stereo = np.stack([left, right], axis=1)
    stereo = np.tanh(stereo * 1.3)
    stereo *= 0.89 / np.max(np.abs(stereo))
    return stereo


def main():
    audio = render()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    pcm = (audio * 32767).astype(np.int16)
    with wave.open(OUT, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    rms = np.sqrt(np.mean(audio ** 2))
    print(f"wrote {OUT}: {N / SR:.1f}s, peak {np.max(np.abs(audio)):.2f}, rms {rms:.3f}")


if __name__ == "__main__":
    main()
