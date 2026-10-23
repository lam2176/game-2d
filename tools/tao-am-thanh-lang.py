"""Tổng hợp âm thanh làng / lò rèn / sổ ghi chép cho Zodiac (không bản quyền, chỉ cần numpy).

Chạy:  python tools/tao-am-thanh-lang.py
Ghi vào Assets/Zodiac/Audio/Synth: anvil_0..2 (búa gõ đe), forge_success (đập thành công), forge_break (đồ vỡ / tụt cấp),
page_0..1 (lật trang), fire_loop (lửa lò rèn lặp liền mạch), ui_click (bấm nút).
"""
import os
import wave

import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Zodiac", "Audio", "Synth")
rng = np.random.default_rng(12)


def t_axis(seconds):
    return np.arange(int(SR * seconds)) / SR


def lowpass(x, cutoff):
    """Lọc thông thấp một cực (đủ để làm mềm tiếng ồn)."""
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def write(name, x, peak=0.9):
    x = np.asarray(x, dtype=np.float64)
    x = x / (np.max(np.abs(x)) + 1e-9) * peak
    data = (x * 32767).astype(np.int16)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    meta = path + ".meta"
    if not os.path.exists(meta):   # Unity tự điền phần cài đặt nhập còn thiếu
        with open(meta, "w", newline="\n") as f:
            f.write(f"fileFormatVersion: 2\nguid: {os.urandom(16).hex()}\n")
    print("ghi", path)


def anvil(base):
    """Búa gõ đe: tiếng kim loại có hoạ âm lệch (không hài hoà), tắt nhanh, kèm tiếng 'cạch' ở đầu."""
    t = t_axis(1.1)
    ratios = [1.0, 2.76, 5.40, 8.93, 13.34]
    amps = [1.0, 0.6, 0.35, 0.2, 0.1]
    x = np.zeros_like(t)
    for r, a in zip(ratios, amps):
        decay = 4.0 + r * 1.6
        x += a * np.sin(2 * np.pi * base * r * t + rng.uniform(0, 6.28)) * np.exp(-decay * t)
    click = rng.normal(0, 1, len(t)) * np.exp(-t * 180)
    x += 0.8 * highpass(click, 2000)
    return x


def forge_success():
    """Đập thành công: ba tiếng chuông lên dần (ngũ cung) + lấp lánh."""
    t = t_axis(1.6)
    x = np.zeros_like(t)
    for i, f in enumerate([784.0, 988.0, 1318.5]):
        start = int(SR * 0.09 * i)
        tt = t[: len(t) - start]
        tone = np.sin(2 * np.pi * f * tt) + 0.35 * np.sin(2 * np.pi * f * 2.0 * tt) + 0.15 * np.sin(2 * np.pi * f * 3.01 * tt)
        x[start:] += tone * np.exp(-tt * 3.2) * (0.7 + 0.15 * i)
    sparkle = highpass(rng.normal(0, 1, len(t)), 6000) * np.exp(-t * 5) * (0.5 + 0.5 * np.sin(2 * np.pi * 23 * t))
    return x + 0.25 * sparkle


def forge_break():
    """Đồ vỡ / tụt cấp: tiếng rắc vỡ giòn rồi tiếng 'bùm' trầm đi xuống."""
    t = t_axis(1.0)
    crack = np.zeros_like(t)
    for _ in range(14):
        s = int(SR * rng.uniform(0.0, 0.25))
        n = int(SR * 0.03)
        burst = rng.normal(0, 1, n) * np.exp(-np.arange(n) / SR * 120)
        crack[s:s + n] += burst[: len(crack) - s] * rng.uniform(0.4, 1.0)
    crack = highpass(crack, 1500)
    freq = 160 * np.exp(-t * 3)
    thud = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-t * 5)
    return crack + 0.9 * thud


def page(seed):
    """Lật trang giấy: tiếng sột soạt (ồn lọc) phồng lên rồi tắt."""
    r = np.random.default_rng(seed)
    t = t_axis(0.45)
    env = np.sin(np.pi * np.clip(t / 0.4, 0, 1)) ** 2
    flutter = 0.6 + 0.4 * np.abs(np.sin(2 * np.pi * r.uniform(28, 40) * t))
    noise = highpass(lowpass(r.normal(0, 1, len(t)), 5000), 800)
    return noise * env * flutter


def fire_loop():
    """Lửa lò rèn: tiếng ầm trầm + lách tách, 6 giây, đầu và cuối trộn chéo để lặp không vấp."""
    seconds, fade = 6.0, 0.5
    t = t_axis(seconds + fade)
    roar = lowpass(rng.normal(0, 1, len(t)), 300) * (0.8 + 0.2 * np.sin(2 * np.pi * 0.3 * t))
    crackle = np.zeros_like(t)
    for _ in range(90):
        s = int(rng.uniform(0, len(t) - 600))
        n = int(rng.uniform(80, 500))
        crackle[s:s + n] += rng.normal(0, 1, n) * np.exp(-np.arange(n) / n * 6) * rng.uniform(0.3, 1.0)
    x = roar * 2.5 + highpass(crackle, 1800)
    n_loop, n_fade = int(SR * seconds), int(SR * fade)
    out = x[:n_loop].copy()
    w = np.linspace(0, 1, n_fade)
    out[:n_fade] = out[:n_fade] * w + x[n_loop:n_loop + n_fade] * (1 - w)
    return out


def ui_click():
    """Bấm nút: tiếng gõ gỗ ngắn (mõ nhỏ)."""
    t = t_axis(0.12)
    x = np.sin(2 * np.pi * 1100 * t) * np.exp(-t * 60) + 0.5 * np.sin(2 * np.pi * 1870 * t) * np.exp(-t * 90)
    return x + 0.3 * highpass(rng.normal(0, 1, len(t)), 3000) * np.exp(-t * 300)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for i, base in enumerate([610.0, 655.0, 580.0]):
        write(f"anvil_{i}", anvil(base))
    write("forge_success", forge_success(), 0.8)
    write("forge_break", forge_break())
    write("page_0", page(1), 0.6)
    write("page_1", page(2), 0.6)
    write("fire_loop", fire_loop(), 0.5)
    write("ui_click", ui_click(), 0.5)
