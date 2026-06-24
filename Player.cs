using Godot;

// 플레이어: 물리 이동 + 점프 손맛 + 더블점프/대시/벽점프 + 주스(스쿼시/파티클/셰이크/히트스톱)
public partial class Player : CharacterBody2D
{
    // 기본 이동
    private const float Speed = 200f;
    private const float Accel = 1400f;
    private const float Friction = 1800f;
    private const float JumpForce = 420f;
    private const float Gravity = 1100f;
    private const float MaxFall = 720f;

    // 손맛
    private const float CoyoteTime = 0.10f;
    private const float JumpBuffer = 0.12f;
    private const float JumpCut = 0.45f;
    private const float BounceForce = 320f;

    // 고급 무브먼트
    private const int MaxJumps = 2;             // 더블 점프
    private const float DashSpeed = 520f;
    private const float DashTime = 0.15f;
    private const float DashCooldown = 0.45f;
    private const float WallSlideSpeed = 90f;   // 벽 미끄럼 시 최대 낙하속도
    private const float WallJumpPushX = 260f;

    public enum State { Idle, Run, Jump, Fall, WallSlide, Dash }
    public State CurrentState { get; private set; } = State.Idle;

    public ShakeCamera Cam; // Game이 주입

    private float _coyoteTimer, _jumpBufferTimer, _dashTimer, _dashCdTimer;
    private int _jumpsLeft;
    private int _dashDir = 1;
    private bool _wasJumpDown, _wasDashDown, _facingLeft, _wallSliding, _wasOnFloor;

    private Vector2 _spawn, _baseScale;
    private AnimatedSprite2D _sprite;
    private AudioStreamPlayer _jumpSfx;
    private Tween _squashTween;

    private static readonly Color Dust = new(0.80f, 0.85f, 0.95f);

    public override void _Ready()
    {
        _spawn = Position;
        AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(24, 36) } });

        _sprite = new AnimatedSprite2D
        {
            SpriteFrames = SpriteFactory.Build(),
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Scale = new Vector2(2.2f, 2.2f),
            Position = new Vector2(0, 2),
        };
        _baseScale = _sprite.Scale;
        AddChild(_sprite);
        _sprite.Play("idle");

        _jumpSfx = new AudioStreamPlayer { Stream = SoundFactory.Jump() };
        AddChild(_jumpSfx);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 v = Velocity;

        // ---- 입력 (엣지 감지) ----
        float xInput =
            (Input.IsPhysicalKeyPressed(Key.Right) || Input.IsPhysicalKeyPressed(Key.D) ? 1f : 0f) -
            (Input.IsPhysicalKeyPressed(Key.Left)  || Input.IsPhysicalKeyPressed(Key.A) ? 1f : 0f);
        bool jumpDown = Input.IsPhysicalKeyPressed(Key.Space) || Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up);
        bool jumpPressed = jumpDown && !_wasJumpDown;
        bool jumpReleased = !jumpDown && _wasJumpDown;
        _wasJumpDown = jumpDown;
        bool dashDown = Input.IsPhysicalKeyPressed(Key.Shift) || Input.IsPhysicalKeyPressed(Key.X);
        bool dashPressed = dashDown && !_wasDashDown;
        _wasDashDown = dashDown;

        // ---- 타이머 ----
        _coyoteTimer = IsOnFloor() ? CoyoteTime : _coyoteTimer - dt;
        _jumpBufferTimer = jumpPressed ? JumpBuffer : _jumpBufferTimer - dt;
        _dashCdTimer -= dt;
        if (IsOnFloor()) _jumpsLeft = MaxJumps;

        // ---- 대시 시작 ----
        if (dashPressed && _dashCdTimer <= 0f && _dashTimer <= 0f)
        {
            _dashTimer = DashTime;
            _dashCdTimer = DashCooldown;
            _dashDir = xInput != 0f ? (int)Mathf.Sign(xInput) : (_facingLeft ? -1 : 1);
            Cam?.Shake(0.35f);
            Fx.Burst(GetParent(), GlobalPosition, new Color(0.6f, 0.9f, 1f), 10, 150f, 50f, new Vector2(-_dashDir, 0));
        }

        bool dashing = _dashTimer > 0f;
        if (dashing)
        {
            _dashTimer -= dt;
            v = new Vector2(_dashDir * DashSpeed, 0f); // 수평 대시(중력 무시)
        }
        else
        {
            // 좌우 이동(가속/마찰)
            v.X = xInput != 0f
                ? Mathf.MoveToward(v.X, xInput * Speed, Accel * dt)
                : Mathf.MoveToward(v.X, 0f, Friction * dt);

            // 중력
            v.Y = Mathf.Min(v.Y + Gravity * dt, MaxFall);

            // 벽 미끄럼
            bool onWall = IsOnWall() && !IsOnFloor();
            _wallSliding = onWall && v.Y > 0f && xInput != 0f;
            if (_wallSliding) v.Y = Mathf.Min(v.Y, WallSlideSpeed);

            // 점프: 벽점프 > 지상/더블점프
            if (_jumpBufferTimer > 0f)
            {
                if (onWall)
                {
                    Vector2 n = GetWallNormal();
                    v.Y = -JumpForce;
                    v.X = n.X * WallJumpPushX;   // 벽 반대쪽으로 튕김
                    _jumpBufferTimer = 0f;
                    _jumpsLeft = MaxJumps - 1;
                    JumpFx(wall: true);
                }
                else if (_coyoteTimer > 0f || _jumpsLeft > 0)
                {
                    bool air = _coyoteTimer <= 0f;
                    v.Y = -JumpForce;
                    _jumpBufferTimer = 0f;
                    if (air) _jumpsLeft--;
                    else { _coyoteTimer = 0f; _jumpsLeft = MaxJumps - 1; }
                    JumpFx(wall: false, doubleJump: air);
                }
            }

            // 가변 점프높이
            if (jumpReleased && v.Y < 0f) v.Y *= JumpCut;
        }

        float fallSpeed = v.Y; // 착지 판정용(미끄러짐 전 속도)
        Velocity = v;
        MoveAndSlide();

        // ---- 적 밟기(충돌 법선) ----
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var col = GetSlideCollision(i);
            if (col.GetCollider() is Enemy enemy)
            {
                if (col.GetNormal().Y < -0.5f) StompEnemy(enemy);
                else if (!dashing) Respawn(); // 대시 중엔 잠깐 무적
            }
        }

        // ---- 착지 효과 ----
        bool onFloorNow = IsOnFloor();
        if (onFloorNow && !_wasOnFloor && fallSpeed > 300f)
        {
            Cam?.Shake(0.18f);
            Squash(new Vector2(1.25f, 0.78f)); // 납작
            Fx.Burst(GetParent(), GlobalPosition + new Vector2(0, 16), Dust, 8, 70f, 150f, Vector2.Up);
        }
        _wasOnFloor = onFloorNow;

        UpdateState(dashing);
        UpdateAnimation(xInput);
    }

    private void StompEnemy(Enemy enemy)
    {
        enemy.Die();
        Velocity = new Vector2(Velocity.X, -BounceForce);
        Cam?.Shake(0.4f);
        Squash(new Vector2(0.8f, 1.25f));
        Fx.Burst(GetParent(), enemy.GlobalPosition, new Color(1f, 0.5f, 0.6f), 14, 170f);
        HitStop(0.06f);
    }

    private void JumpFx(bool wall, bool doubleJump = false)
    {
        _jumpSfx.Play();
        Squash(new Vector2(0.78f, 1.25f)); // 길쭉
        Color c = doubleJump ? new Color(0.7f, 1f, 0.8f) : Dust;
        Fx.Burst(GetParent(), GlobalPosition + new Vector2(0, 16), c, doubleJump ? 10 : 6, 90f, wall ? 110f : 160f, Vector2.Up);
        if (doubleJump) Cam?.Shake(0.12f);
    }

    // 스쿼시&스트레치: 즉시 변형 후 원래 크기로 탄력있게 복귀
    private void Squash(Vector2 ratio)
    {
        _squashTween?.Kill();
        _sprite.Scale = _baseScale * ratio;
        _squashTween = CreateTween();
        _squashTween.TweenProperty(_sprite, "scale", _baseScale, 0.16f)
                    .SetTrans(Tween.TransitionType.Back)
                    .SetEase(Tween.EaseType.Out);
    }

    // 히트스톱: 잠깐 시간 거의 멈춤 → 타격감. 실시간 타이머로 복구
    private void HitStop(float seconds)
    {
        Engine.TimeScale = 0.05;
        GetTree().CreateTimer(seconds, true, false, true).Timeout += () => Engine.TimeScale = 1.0;
    }

    private void UpdateState(bool dashing)
    {
        if (dashing) CurrentState = State.Dash;
        else if (_wallSliding) CurrentState = State.WallSlide;
        else if (!IsOnFloor()) CurrentState = Velocity.Y < 0f ? State.Jump : State.Fall;
        else CurrentState = Mathf.Abs(Velocity.X) > 10f ? State.Run : State.Idle;
    }

    private void UpdateAnimation(float xInput)
    {
        if (xInput < 0f) _facingLeft = true;
        else if (xInput > 0f) _facingLeft = false;
        _sprite.FlipH = _facingLeft;

        string anim = CurrentState switch
        {
            State.Dash => "jump",
            State.WallSlide => "fall",
            State.Jump => "jump",
            State.Fall => "fall",
            State.Run => "run",
            _ => "idle",
        };
        if ((string)_sprite.Animation != anim)
            _sprite.Play(anim);
    }

    public void Respawn()
    {
        Position = _spawn;
        Velocity = Vector2.Zero;
        _dashTimer = 0f;
        Engine.TimeScale = 1.0; // 히트스톱 중 죽어도 복구
    }
}
