using Godot;

// 일회성 파티클 버스트 헬퍼. 부모에 CpuParticles2D를 붙여 한 번 터뜨리고 자동 제거.
public static class Fx
{
    public static void Burst(Node parent, Vector2 pos, Color color, int amount,
                             float speed, float spread = 180f, Vector2? dir = null)
    {
        var p = new CpuParticles2D
        {
            Position = pos,
            Emitting = true,
            OneShot = true,
            Explosiveness = 1f,        // 한 번에 다 뿜기
            Amount = amount,
            Lifetime = 0.5,
            Direction = dir ?? Vector2.Up,
            Spread = spread,
            Gravity = new Vector2(0, 340),
            InitialVelocityMin = speed * 0.4f,
            InitialVelocityMax = speed,
            ScaleAmountMin = 2f,
            ScaleAmountMax = 4f,
            Color = color,
        };
        parent.AddChild(p);
        parent.GetTree().CreateTimer(1.2).Timeout += p.QueueFree; // 수명 후 정리
    }
}
