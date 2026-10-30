"""Genera música y efectos originales. Ejecutar con Python 3, sin paquetes externos."""
from array import array
from pathlib import Path
import math
import random
import sys
import wave

RATE = 44100
OUTPUT = Path(__file__).resolve().parents[1] / "Assets/Resources/RetroAudio"


def frequency(note):
    return 440 * 2 ** ((note - 69) / 12)


def pulse(phase):
    # Aproximación suave: evita las discontinuidades de una onda cuadrada ideal.
    return (math.sin(phase) + math.sin(phase * 3) / 3 + math.sin(phase * 5) / 5) / 1.53


def write(name, samples):
    peak = max(1.0, max(abs(v) for v in samples))
    pcm = array("h", (int(max(-1, min(1, v / peak)) * 26000) for v in samples))
    if sys.byteorder != "little":
        pcm.byteswap()
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as wav:
        wav.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        wav.writeframes(pcm.tobytes())


def effect(name, notes, step=0.075, sweep=0.0, noise=False):
    rng = random.Random(42)
    samples = []
    phase = 0.0
    for note in notes:
        for i in range(int(step * RATE)):
            t = i / RATE
            env = min(1, t / 0.004) * max(0, 1 - t / step) ** 0.6
            phase += 2 * math.pi * frequency(note) * (1 + sweep * t / step) / RATE
            signal = 0.65 * pulse(phase) + (0.25 * rng.uniform(-1, 1) if noise else 0)
            samples.append(signal * env)
    write(name, samples)


def music(name, melody, bass, beat):
    samples = []
    for index, note in enumerate(melody):
        low = bass[(index // 8) % len(bass)]
        for i in range(int(beat * RATE)):
            t = i / RATE
            env = min(1, t / 0.006) * max(0, 1 - t / beat) ** 0.55
            lead = 0 if note == 0 else pulse(2 * math.pi * frequency(note) * t) * 0.36
            accompaniment = math.sin(2 * math.pi * frequency(low) * t) * 0.23
            # Todas las notas terminan en cero: el bucle no produce un clic.
            samples.append((lead + accompaniment) * env)
    write(name, samples)


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    effect("Jump", [48, 55, 62], 0.045, 0.25)
    effect("Coin", [88, 95], 0.10)
    effect("Bump", [40, 35], 0.045)
    effect("Break", [45, 38, 30], 0.075, -0.3, True)
    effect("Item", [52, 56, 59, 64, 68, 71], 0.055)
    effect("PowerUp", [60, 64, 67, 72, 76, 79], 0.07)
    effect("Damage", [76, 69, 62, 55], 0.07, -0.1)
    effect("Stomp", [43, 34], 0.06, -0.2)
    effect("Kick", [55, 43], 0.065, -0.3, True)
    effect("Pipe", [55, 48, 43, 55, 48, 43], 0.065)
    effect("ExtraLife", [67, 72, 76, 79, 84], 0.10)
    effect("Death", [72, 67, 63, 60, 55, 48], 0.16)
    effect("Complete", [60, 65, 69, 72, 77, 81, 84], 0.14)
    surface = [72, 0, 76, 79, 77, 76, 72, 67, 69, 72, 74, 77, 76, 72, 67, 0,
               74, 0, 77, 81, 79, 77, 74, 69, 71, 74, 76, 79, 77, 74, 72, 0]
    underground = [48, 55, 60, 0, 51, 58, 63, 0, 46, 53, 58, 0, 50, 57, 62, 0,
                   48, 60, 55, 0, 51, 63, 58, 0, 46, 58, 53, 0, 55, 50, 48, 0]
    star = [79, 84, 88, 86, 81, 86, 89, 88, 77, 81, 86, 84, 79, 83, 86, 88] * 2
    music("Surface", surface, [48, 45, 50, 43], 0.25)
    music("Underground", underground, [36, 39, 34, 38], 0.25)
    music("Star", star, [48, 50, 53, 55], 0.15)
    print(f"16 clips generados en {OUTPUT}")


if __name__ == "__main__":
    main()
