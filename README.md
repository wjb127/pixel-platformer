# Pixel Platformer 🟦

Godot 4.7 (.NET / C#) 로 만든 2D 플랫포머. 외부 에셋 없이 **코드로 도형/스프라이트/사운드를 모두 생성**해서 의존성 0으로 동작한다. 게임 개발 핵심 시스템을 학습하기 위한 프로젝트.

## 플레이 방법

- **이동**: 방향키 또는 `A` / `D`
- **점프**: `Space` / `W` / `↑`
- **적 처치**: 적을 **위에서 밟기** (옆/아래로 닿으면 리스폰)
- **목표**: 코인을 모으고 초록 깃발에 도착
- **재시작**: 클리어 후 `R`

## 구현된 기술

- **CharacterBody2D 물리** — 중력, 가속/마찰, `MoveAndSlide`
- **점프 손맛 3종** — 코요테 타임, 점프 버퍼, 가변 점프 높이
- **상태 머신(FSM)** — Idle / Run / Jump / Fall
- **Area2D + 커스텀 시그널** — 코인/목표/위험지대 감지
- **적 AI + 밟기** — 좌우 순찰, 충돌 법선으로 위/옆 판정
- **Camera2D** — 부드러운 플레이어 추적
- **CanvasLayer UI** — 코인/처치 수, 클리어 메시지

## 실행

1. [Godot 4.7 .NET (C#)](https://godotengine.org/download) + [.NET SDK 8+](https://dotnet.microsoft.com/download)
2. 클론 후 Godot에서 폴더 Import
   ```bash
   git clone https://github.com/<your-id>/pixel-platformer.git
   ```
3. `F5`로 실행 (첫 실행 시 C# 솔루션 자동 빌드)

명령줄 실행:
```bash
godot --path pixel-platformer
```

## 구조

| 파일 | 역할 |
|------|------|
| `Game.cs` | 레벨 생성, 카메라, UI, 시그널 연결 |
| `Player.cs` | 물리 이동, 점프 손맛, FSM, 적 밟기 |
| `Enemy.cs` | 좌우 순찰 적 |
| `Coin.cs` / `Goal.cs` / `Hazard.cs` | Area2D + 시그널 트리거 |

## 튜닝

`Player.cs` 상단 상수(`JumpForce`, `Gravity`, `Speed`, `CoyoteTime` 등)를 바꾸면 조작감이 달라진다.

## 라이선스

MIT
