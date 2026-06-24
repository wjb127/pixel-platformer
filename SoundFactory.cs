using Godot;

// 외부 음원 없이 코드로 PCM을 합성해 효과음 생성 (jsfxr/Bfxr가 하는 일을 직접).
// 16비트 모노 PCM을 만들어 AudioStreamWav로 반환.
public static class SoundFactory
{
    private const int Rate = 22050;

    public static AudioStreamWav Jump()  => Build(0.40f, true, (320f, 0.05f), (560f, 0.08f));
    public static AudioStreamWav Coin()  => Build(0.32f, true, (880f, 0.04f), (1320f, 0.09f));
    public static AudioStreamWav Stomp() => Build(0.45f, true, (300f, 0.04f), (110f, 0.11f));
    public static AudioStreamWav Win()   => Build(0.38f, true, (523f, 0.10f), (659f, 0.10f), (784f, 0.10f), (1047f, 0.18f));

    // (주파수, 길이) 음들을 이어붙여 하나의 효과음으로
    private static AudioStreamWav Build(float vol, bool square, params (float freq, float dur)[] notes)
    {
        int total = 0;
        foreach (var note in notes)
            total += (int)(Rate * note.dur);

        var data = new byte[total * 2]; // 16bit = 샘플당 2바이트
        int idx = 0;
        double phase = 0.0;

        foreach (var (freq, dur) in notes)
        {
            int n = (int)(Rate * dur);
            for (int i = 0; i < n; i++)
            {
                phase += Mathf.Tau * freq / Rate;
                if (phase >= Mathf.Tau) phase -= Mathf.Tau;

                float raw = Mathf.Sin((float)phase);
                float wave = square ? (raw >= 0f ? 1f : -1f) : raw; // 사각파/사인파
                float env = 1f - (float)i / n;                      // 선형 감쇠(클릭 방지)

                short s = (short)(wave * env * vol * 32767f);
                data[idx++] = (byte)(s & 0xFF);        // little-endian 하위 바이트
                data[idx++] = (byte)((s >> 8) & 0xFF); // 상위 바이트
            }
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = Rate,
            Stereo = false,
            Data = data,
        };
    }
}
