using Godot;

// 외부 이미지 없이 코드로 픽셀 캐릭터 프레임을 생성 → SpriteFrames로 묶음.
// (실무에선 .png 스프라이트시트를 임포트하지만, AnimatedSprite2D 사용 파이프라인은 동일)
public static class SpriteFactory
{
    private const int W = 14;
    private const int H = 16;

    private static readonly Color Body  = new(0.40f, 0.80f, 0.95f);
    private static readonly Color Edge  = new(0.20f, 0.50f, 0.70f);
    private static readonly Color Leg   = new(0.18f, 0.26f, 0.40f);
    private static readonly Color White = Colors.White;
    private static readonly Color Black = Colors.Black;
    private static readonly Color Clear = new(0, 0, 0, 0);

    // idle/run/jump/fall 4종 애니메이션이 담긴 SpriteFrames 반환
    public static SpriteFrames Build()
    {
        var sf = new SpriteFrames();
        sf.RemoveAnimation("default"); // 기본 빈 애니 제거

        AddAnim(sf, "idle", 3f,  true,  Frame(0, false), Frame(0, false, bob: true));
        AddAnim(sf, "run",  10f, true,  Frame(0, false), Frame(1, false));
        AddAnim(sf, "jump", 1f,  false, Frame(0, true));
        AddAnim(sf, "fall", 1f,  false, Frame(1, true));
        return sf;
    }

    private static void AddAnim(SpriteFrames sf, string name, float fps, bool loop, params Texture2D[] frames)
    {
        sf.AddAnimation(name);
        sf.SetAnimationSpeed(name, fps);
        sf.SetAnimationLoopMode(name, loop ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);
        foreach (var f in frames)
            sf.AddFrame(name, f);
    }

    // 한 프레임 텍스처를 픽셀 단위로 그림
    private static ImageTexture Frame(int legPhase, bool airborne, bool bob = false)
    {
        var img = Image.CreateEmpty(W, H, false, Image.Format.Rgba8);
        img.Fill(Clear);
        int top = bob ? 1 : 0; // idle 살짝 들썩

        // 몸통(둥근 사각형): 가장자리는 어둡게
        for (int y = 1; y <= 10; y++)
        for (int x = 2; x <= 11; x++)
        {
            bool corner = (x == 2 || x == 11) && (y == 1 || y == 10);
            if (corner) continue;
            bool edge = x == 2 || x == 11 || y == 1 || y == 10;
            img.SetPixel(x, y + top, edge ? Edge : Body);
        }

        // 눈
        img.SetPixel(5, 4 + top, White);
        img.SetPixel(9, 4 + top, White);
        img.SetPixel(5, 5 + top, Black);
        img.SetPixel(9, 5 + top, Black);

        // 다리
        if (airborne)
        {
            FillRect(img, 4, 11 + top, 2, 2, Leg); // 점프/낙하: 짧게 모음
            FillRect(img, 8, 11 + top, 2, 2, Leg);
        }
        else
        {
            int lh = legPhase == 0 ? 3 : 1; // 걷기: 좌우 다리 길이 교차
            int rh = legPhase == 0 ? 1 : 3;
            FillRect(img, 4, 11 + top, 2, lh, Leg);
            FillRect(img, 8, 11 + top, 2, rh, Leg);
        }

        return ImageTexture.CreateFromImage(img);
    }

    private static void FillRect(Image img, int x, int y, int w, int h, Color c)
    {
        for (int yy = y; yy < y + h && yy < H; yy++)
        for (int xx = x; xx < x + w && xx < W; xx++)
            if (xx >= 0 && yy >= 0)
                img.SetPixel(xx, yy, c);
    }
}
