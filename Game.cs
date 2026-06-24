using Godot;
using System.Collections.Generic;

// 게임 매니저: 레벨 생성(발판/코인/위험/목표), 카메라, UI, 시그널 연결, 추락/클리어 처리
public partial class Game : Node2D
{
    private Player _player;
    private readonly List<Rect2> _platforms = new(); // 그리기용으로 발판 사각형 저장
    private int _coinsTotal;
    private int _coinsGot;
    private int _kills;
    private bool _won;
    private const float DeathY = 560f; // 이 아래로 떨어지면 리스폰(안전망)

    private Label _coinLabel;
    private Label _msgLabel;

    public override void _Ready()
    {
        BuildLevel();
        SetupCamera();
        SetupUI();
        UpdateUI();
    }

    // ===== 레벨 구성 =====
    private void BuildLevel()
    {
        // 발판 (x, y, 너비, 높이) — 점프 가능 범위에 맞춰 배치
        AddPlatform(0, 320, 220, 40);     // 시작 바닥
        AddPlatform(300, 300, 120, 20);
        AddPlatform(480, 250, 120, 20);
        AddPlatform(660, 300, 120, 20);
        AddPlatform(840, 320, 280, 40);   // 목표가 있는 바닥
        AddPlatform(1160, 270, 110, 20);  // 보너스 발판

        // 위험지대(용암): 발판 아래 구덩이를 가로지르는 빨간 띠
        AddChild(new Hazard
        {
            Position = new Vector2(640, 420),
            RectSize = new Vector2(1400, 40),
        });

        // 플레이어 (스폰 위치)
        _player = new Player { Position = new Vector2(60, 280) };
        AddChild(_player);

        // 코인 (각 발판 위)
        AddCoin(120, 285);
        AddCoin(360, 265);
        AddCoin(540, 215);
        AddCoin(720, 265);
        AddCoin(1215, 235);

        // 적 (x, y, 순찰 왼쪽경계, 오른쪽경계) — 발판 위를 왔다갔다
        AddEnemy(360, 285, 305, 415);    // 두 번째 발판 위
        AddEnemy(950, 300, 850, 1090);   // 목표 바닥 위 (깃발 앞을 지킴)

        // 목표 깃발
        var goal = new Goal { Position = new Vector2(1010, 296) };
        goal.Reached += OnGoalReached;  // 시그널 연결
        AddChild(goal);
    }

    private void AddPlatform(float x, float y, float w, float h)
    {
        var r = new Rect2(x, y, w, h);
        var body = new StaticBody2D { Position = r.Position + r.Size / 2f };
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = r.Size } });
        AddChild(body);
        _platforms.Add(r); // _Draw에서 그림
    }

    private void AddCoin(float x, float y)
    {
        var coin = new Coin { Position = new Vector2(x, y) };
        coin.Collected += OnCoinCollected; // 시그널 연결
        AddChild(coin);
        _coinsTotal++;
    }

    private void AddEnemy(float x, float y, float left, float right)
    {
        var enemy = new Enemy { Position = new Vector2(x, y), LeftLimit = left, RightLimit = right };
        enemy.Defeated += OnEnemyDefeated; // 시그널 연결
        AddChild(enemy);
    }

    // ===== 카메라: 플레이어 자식으로 붙이면 자동 추적 =====
    private void SetupCamera()
    {
        var cam = new Camera2D
        {
            PositionSmoothingEnabled = true, // 부드럽게 따라옴
            PositionSmoothingSpeed = 6f,
        };
        _player.AddChild(cam);
        cam.MakeCurrent();
    }

    // ===== UI: CanvasLayer는 화면 고정(카메라 영향 X) =====
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
        _msgLabel.AddThemeFontSizeOverride("font_size", 28);
        ui.AddChild(_msgLabel);
    }

    public override void _Process(double delta)
    {
        if (_won)
        {
            if (Input.IsPhysicalKeyPressed(Key.R))
                GetTree().ReloadCurrentScene();
            return;
        }

        // 추락 안전망
        if (_player.Position.Y > DeathY)
            _player.Respawn();
    }

    // ===== 시그널 콜백 =====
    private void OnCoinCollected()
    {
        _coinsGot++;
        UpdateUI();
    }

    private void OnEnemyDefeated()
    {
        _kills++;
        UpdateUI();
    }

    private void OnGoalReached()
    {
        _won = true;
        _msgLabel.Text = $"클리어!   코인 {_coinsGot}/{_coinsTotal}  ·  적 {_kills}\nR 키로 다시 시작";
        _msgLabel.Visible = true;
    }

    private void UpdateUI() => _coinLabel.Text = $"코인 {_coinsGot}/{_coinsTotal}    적 {_kills}";

    // 발판들을 그림(StaticBody2D는 스스로 안 그리므로 여기서)
    public override void _Draw()
    {
        var top = new Color(0.45f, 0.5f, 0.62f);
        var body = new Color(0.24f, 0.28f, 0.38f);
        foreach (var r in _platforms)
        {
            DrawRect(r, body);                                   // 몸통
            DrawRect(new Rect2(r.Position, new Vector2(r.Size.X, 4)), top); // 윗면 하이라이트
        }
    }
}
