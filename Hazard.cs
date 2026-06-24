using Godot;

// 위험지대(용암): 플레이어가 닿으면 리스폰시킴
public partial class Hazard : Area2D
{
    public Vector2 RectSize = new(100, 30);

    public override void _Ready()
    {
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = RectSize } });
        BodyEntered += body =>
        {
            if (body is Player p)
                p.Respawn();
        };
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(-RectSize / 2f, RectSize), new Color(0.90f, 0.22f, 0.20f, 0.75f));
    }
}
