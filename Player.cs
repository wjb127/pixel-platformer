using Godot;

// 플레이어: CharacterBody2D 물리 이동 + 점프 손맛(코요테/버퍼/가변높이) + 상태머신
public partial class Player : CharacterBody2D
{
    // ===== 튜닝 값 (여기 숫자만 바꿔도 느낌이 확 달라짐) =====
    private const float Speed = 200f;      // 최대 이동 속도
    private const float Accel = 1400f;     // 가속(누를 때)
    private const float Friction = 1800f;  // 감속(뗄 때)
    private const float JumpForce = 420f;  // 점프 초기 속도
    private const float Gravity = 1100f;   // 중력
    private const float MaxFall = 720f;    // 최대 낙하 속도

    // 손맛 3종
    private const float CoyoteTime = 0.10f;        // 발판 떠난 직후에도 잠깐 점프 허용
    private const float JumpBuffer = 0.12f;        // 착지 직전 점프 입력을 기억
    private const float JumpCut = 0.45f;           // 점프 버튼 일찍 떼면 짧게(가변 높이)
    private const float BounceForce = 320f;        // 적을 밟았을 때 통통 튕기는 힘

    // ===== 상태머신 =====
    public enum State { Idle, Run, Jump, Fall }
    public State CurrentState { get; private set; } = State.Idle;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _wasJumpDown;
    private Vector2 _spawn;

    public override void _Ready()
    {
        _spawn = Position;
        // 충돌 모양을 코드로 부착 (24x36 사각형)
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(24, 36) } });
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 v = Velocity;

        // --- 입력 (간단하게 물리키 폴링. 나중에 Input Map으로 바꾸면 게임패드/리매핑 가능) ---
        float xInput =
            (Input.IsPhysicalKeyPressed(Key.Right) || Input.IsPhysicalKeyPressed(Key.D) ? 1f : 0f) -
            (Input.IsPhysicalKeyPressed(Key.Left)  || Input.IsPhysicalKeyPressed(Key.A) ? 1f : 0f);

        bool jumpDown = Input.IsPhysicalKeyPressed(Key.Space)
                     || Input.IsPhysicalKeyPressed(Key.W)
                     || Input.IsPhysicalKeyPressed(Key.Up);
        bool jumpPressed = jumpDown && !_wasJumpDown;   // 이번 프레임에 막 누름
        bool jumpReleased = !jumpDown && _wasJumpDown;  // 이번 프레임에 막 뗌
        _wasJumpDown = jumpDown;

        // --- 좌우 이동 (가속/마찰) ---
        v.X = xInput != 0f
            ? Mathf.MoveToward(v.X, xInput * Speed, Accel * dt)
            : Mathf.MoveToward(v.X, 0f, Friction * dt);

        // --- 중력 ---
        v.Y = Mathf.Min(v.Y + Gravity * dt, MaxFall);

        // --- 코요테 타임: 바닥에 있으면 타이머 충전, 떨어지면 감소 ---
        _coyoteTimer = IsOnFloor() ? CoyoteTime : _coyoteTimer - dt;

        // --- 점프 버퍼: 누른 입력을 잠깐 기억 ---
        _jumpBufferTimer = jumpPressed ? JumpBuffer : _jumpBufferTimer - dt;

        // --- 실제 점프: 버퍼와 코요테가 둘 다 살아있을 때 ---
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            v.Y = -JumpForce;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }
        // --- 가변 점프 높이: 상승 중 버튼 떼면 위로 가던 속도를 깎음 ---
        if (jumpReleased && v.Y < 0f)
            v.Y *= JumpCut;

        Velocity = v;
        MoveAndSlide();   // CharacterBody2D가 발판과의 충돌/미끄러짐을 알아서 처리

        // --- 적 밟기 판정: 충돌 '법선'으로 위/옆 구분 (마리오 방식) ---
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var col = GetSlideCollision(i);
            if (col.GetCollider() is Enemy enemy)
            {
                if (col.GetNormal().Y < -0.5f) // 법선이 위쪽 → 위에서 밟음
                {
                    enemy.Die();
                    Velocity = new Vector2(Velocity.X, -BounceForce); // 통통 튕기기
                }
                else // 옆/아래에서 닿음 → 데미지(리스폰)
                {
                    Respawn();
                }
            }
        }

        UpdateState();
        QueueRedraw();
    }

    // 속도/접지 상태로 현재 상태 결정
    private void UpdateState()
    {
        if (!IsOnFloor())
            CurrentState = Velocity.Y < 0f ? State.Jump : State.Fall;
        else
            CurrentState = Mathf.Abs(Velocity.X) > 10f ? State.Run : State.Idle;
    }

    // 추락/위험지대에서 호출
    public void Respawn()
    {
        Position = _spawn;
        Velocity = Vector2.Zero;
    }

    public override void _Draw()
    {
        // 상태별 색으로 FSM을 눈으로 확인
        Color c = CurrentState switch
        {
            State.Jump => new Color(0.50f, 0.90f, 1.00f),
            State.Fall => new Color(1.00f, 0.70f, 0.40f),
            State.Run  => new Color(0.40f, 0.95f, 0.60f),
            _          => new Color(0.72f, 0.85f, 1.00f),
        };
        DrawRect(new Rect2(-12, -18, 24, 36), c);
        DrawCircle(new Vector2(-4, -8), 2.5f, Colors.Black);
        DrawCircle(new Vector2(5, -8), 2.5f, Colors.Black);
    }
}
