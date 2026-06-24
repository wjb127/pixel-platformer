using Godot;

// 코인: Area2D로 플레이어 진입 감지 → 커스텀 시그널 Collected 발신 후 제거
public partial class Coin : Area2D
{
    [Signal] public delegate void CollectedEventHandler();

    private float _t;

    public override void _Ready()
    {
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 9f } });
        BodyEntered += OnBodyEntered; // 물리 바디가 영역에 들어오면 호출
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Player)
        {
            Fx.Burst(GetParent(), GlobalPosition, new Color(1f, 0.9f, 0.4f), 8, 100f); // 반짝임
            EmitSignal(SignalName.Collected); // Game이 이 시그널을 듣고 점수 +1
            QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        _t += (float)delta * 4f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float r = 9f * (1f + Mathf.Sin(_t) * 0.12f);
        DrawCircle(Vector2.Zero, r, new Color(1f, 0.84f, 0.20f));
        DrawCircle(Vector2.Zero, r * 0.5f, new Color(1f, 0.96f, 0.65f));
    }
}
