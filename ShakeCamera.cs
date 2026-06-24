using Godot;

// 트라우마 기반 화면 흔들기 카메라. Shake(amount)로 트라우마 누적 → 제곱으로 감쇠.
// (선형 감쇠보다 자연스러움 — 약한 충격은 더 약하게, 강한 충격은 확 흔들림)
public partial class ShakeCamera : Camera2D
{
    private const float MaxOffset = 9f;
    private const float Decay = 1.5f;
    private float _trauma;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready() => _rng.Randomize();

    public void Shake(float amount) => _trauma = Mathf.Min(_trauma + amount, 1f);

    public override void _Process(double delta)
    {
        if (_trauma > 0f)
        {
            _trauma = Mathf.Max(_trauma - Decay * (float)delta, 0f);
            float s = _trauma * _trauma; // 제곱
            Offset = new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-1f, 1f)) * MaxOffset * s;
        }
        else if (Offset != Vector2.Zero)
        {
            Offset = Vector2.Zero;
        }
    }
}
