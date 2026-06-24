using Godot;

// 적: 좌우 순찰(CharacterBody2D + 중력). 밟기 판정은 Player가 충돌 법선으로 처리.
public partial class Enemy : CharacterBody2D
{
    [Signal] public delegate void DefeatedEventHandler();

    private const float Speed = 55f;
    private const float Gravity = 1100f;

    public float LeftLimit;   // 순찰 왼쪽 경계
    public float RightLimit;  // 순찰 오른쪽 경계
    private int _dir = 1;

    public override void _Ready()
    {
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(26, 26) } });
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 v = Velocity;
        v.X = _dir * Speed;
        v.Y = Mathf.Min(v.Y + Gravity * dt, 700f); // 중력으로 발판 위에 안착
        Velocity = v;
        MoveAndSlide();

        // 순찰 경계에서 방향 전환
        if ((_dir > 0 && Position.X >= RightLimit) || (_dir < 0 && Position.X <= LeftLimit))
            _dir = -_dir;

        QueueRedraw();
    }

    public void Die()
    {
        EmitSignal(SignalName.Defeated);
        QueueFree();
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-13, -13, 26, 26), new Color(0.95f, 0.45f, 0.55f));
        DrawCircle(new Vector2(-5, -3), 2.5f, Colors.Black);
        DrawCircle(new Vector2(5, -3), 2.5f, Colors.Black);
        DrawLine(new Vector2(-5, 6), new Vector2(5, 6), Colors.Black, 1.5f); // 찡그린 입
    }
}
