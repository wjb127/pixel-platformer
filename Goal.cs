using Godot;

// 목표 깃발: 플레이어가 닿으면 Reached 시그널 발신
public partial class Goal : Area2D
{
    [Signal] public delegate void ReachedEventHandler();

    public override void _Ready()
    {
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 48) } });
        BodyEntered += body =>
        {
            if (body is Player)
                EmitSignal(SignalName.Reached);
        };
    }

    public override void _Draw()
    {
        // 깃대
        DrawRect(new Rect2(-2, -24, 4, 48), new Color(0.65f, 0.65f, 0.7f));
        // 깃발(삼각형)
        DrawColoredPolygon(
            new Vector2[] { new(2, -24), new(22, -16), new(2, -8) },
            new Color(0.30f, 0.90f, 0.45f));
    }
}
