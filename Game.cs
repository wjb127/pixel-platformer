using Godot;
using System.Collections.Generic;

// 게임 매니저: 레벨 생성, 카메라(셰이크), UI, 시그널, 추락/클리어, 사운드
public partial class Game : Node2D
{
    private Player _player;
    private readonly List<Rect2> _platforms = new();
    private int _coinsTotal;
    private int _coinsGot;
    private int _kills;
    private bool _won;
    private const float DeathY = 560f;

    private Label _coinLabel;
    private Label _msgLabel;
    private ShakeCamera _cam;
    private AudioStreamPlayer _coinSfx, _stompSfx, _winSfx;

    public override void _Ready()
    {
        BuildLevel();
        SetupCamera();
        SetupUI();
        SetupAudio();
        UpdateUI();
    }

    // ===== 레벨 =====
    private void BuildLevel()
    {
        // 기본 발판
        AddPlatform(0, 320, 220, 40);
        AddPlatform(300, 300, 120, 20);
        AddPlatform(480, 250, 120, 20);
        AddPlatform(660, 300, 120, 20);
        AddPlatform(840, 320, 280, 40);
        AddPlatform(900, 175, 90, 20);     // 더블점프용 높은 발판
        AddPlatform(1320, 300, 200, 40);   // 대시용 먼 발판 (넓은 간격)
        AddPlatform(1500, 150, 24, 190);   // 벽점프/슬라이드 연습용 높은 벽

        // 위험지대(용암)
        AddChild(new Hazard { Position = new Vector2(640, 430), RectSize = new Vector2(1700, 40) });

        // 플레이어
        _player = new Player { Position = new Vector2(60, 280) };
        AddChild(_player);

        // 코인
        AddCoin(120, 285);
        AddCoin(360, 265);
        AddCoin(540, 215);
        AddCoin(720, 265);
        AddCoin(945, 150);   // 더블점프 보너스
        AddCoin(1190, 245);  // 대시 간격 중간(공중에서 낚아채기)
        AddCoin(1400, 265);  // 먼 발판

        // 적
        AddEnemy(360, 285, 305, 415);
        AddEnemy(950, 300, 850, 1090);

        // 목표 (먼 발판 위, 벽 앞)
        var goal = new Goal { Position = new Vector2(1450, 276) };
        goal.Reached += OnGoalReached;
        AddChild(goal);
    }

    private void AddPlatform(float x, float y, float w, float h)
    {
        var r = new Rect2(x, y, w, h);
        var body = new StaticBody2D { Position = r.Position + r.Size / 2f };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = r.Size } });
        AddChild(body);
        _platforms.Add(r);
    }

    private void AddCoin(float x, float y)
    {
        var coin = new Coin { Position = new Vector2(x, y) };
        coin.Collected += OnCoinCollected;
        AddChild(coin);
        _coinsTotal++;
    }

    private void AddEnemy(float x, float y, float left, float right)
    {
        var enemy = new Enemy { Position = new Vector2(x, y), LeftLimit = left, RightLimit = right };
        enemy.Defeated += OnEnemyDefeated;
        AddChild(enemy);
    }

    // ===== 카메라 (흔들림) =====
    private void SetupCamera()
    {
        _cam = new ShakeCamera { PositionSmoothingEnabled = true, PositionSmoothingSpeed = 6f };
        _player.AddChild(_cam);
        _cam.MakeCurrent();
        _player.Cam = _cam; // 플레이어가 대시/착지/밟기에서 흔들 수 있게 주입
    }

    // ===== UI =====
    private void SetupUI()
    {
        var ui = new CanvasLayer();
        AddChild(ui);

        _coinLabel = new Label { Position = new Vector2(12, 8) };
        _coinLabel.AddThemeFontSizeOverride("font_size", 20);
        ui.AddChild(_coinLabel);

        _msgLabel = new Label
        {
            Position = new Vector2(0, 150),
            Size = new Vector2(640, 60),
            Visible = false,
        };
        _msgLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _msgLabel.VerticalAlignment = VerticalAlignment.Center;
        _msgLabel.AddThemeFontSizeOverride("font_size", 28);
        ui.AddChild(_msgLabel);
    }

    // ===== 오디오 =====
    private void SetupAudio()
    {
        _coinSfx  = AddSfx(SoundFactory.Coin());
        _stompSfx = AddSfx(SoundFactory.Stomp());
        _winSfx   = AddSfx(SoundFactory.Win());
    }

    private AudioStreamPlayer AddSfx(AudioStream stream)
    {
        var p = new AudioStreamPlayer { Stream = stream };
        AddChild(p);
        return p;
    }

    public override void _Process(double delta)
    {
        if (_won)
        {
            if (Input.IsPhysicalKeyPressed(Key.R))
                GetTree().ReloadCurrentScene();
            return;
        }
        if (_player.Position.Y > DeathY)
            _player.Respawn();
    }

    // ===== 시그널 콜백 =====
    private void OnCoinCollected()
    {
        _coinsGot++;
        _coinSfx.Play();
        _cam?.Shake(0.08f);
        UpdateUI();
    }

    private void OnEnemyDefeated()
    {
        _kills++;
        _stompSfx.Play();
        UpdateUI();
    }

    private void OnGoalReached()
    {
        _won = true;
        _winSfx.Play();
        _cam?.Shake(0.5f);
        _msgLabel.Text = $"클리어!   코인 {_coinsGot}/{_coinsTotal}  ·  적 {_kills}\nR 키로 다시 시작";
        _msgLabel.Visible = true;
    }

    private void UpdateUI() => _coinLabel.Text = $"코인 {_coinsGot}/{_coinsTotal}    적 {_kills}";

    public override void _Draw()
    {
        var top = new Color(0.45f, 0.5f, 0.62f);
        var body = new Color(0.24f, 0.28f, 0.38f);
        foreach (var r in _platforms)
        {
            DrawRect(r, body);
            DrawRect(new Rect2(r.Position, new Vector2(r.Size.X, 4)), top);
        }
    }
}
