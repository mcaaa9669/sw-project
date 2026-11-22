# SHELL 숲속 대포 — 팀원용 사용 안내

## 바로 사용

1. develop 최신화 후 Unity에서 `Assets/SHELL_ForestMechanics_20261122/Prefabs/SHELL_ForestCannon.prefab`을 찾습니다.
2. 프리팹을 원하는 스테이지 Hierarchy/Scene에 끌어 놓습니다.
3. 고양이가 대포의 충돌 영역에 닿으면 자동 발사됩니다. 버튼·외부 스크립트 연결 불필요.

## Inspector 설정

- `Launch Speed` (기본 9.2): 클수록 멀리 날아갑니다.
- `Launch Angle` (기본 48°): 발사 각도를 조절합니다.
- `Fire To Right`: 체크하면 오른쪽, 해제하면 왼쪽으로 발사됩니다. 왼쪽 발사 시 그림 방향은 `SpriteRenderer.flipX`로 맞춥니다.
- `Arc Gravity` (기본 9.81): 클수록 비행 시간이 짧아집니다.
- `Reentry Cooldown`: 연속 재발사 방지 시간.
- `Only Player`: 현재 설정이 꺼져 있으면 다른 Dynamic Rigidbody2D도 발사될 수 있습니다. 고양이만 원한다면 켜고 플레이어 인식 여부를 검증하세요.

## 프리팹 구성

- `ForestCannon.png`: 대포 아트, 256×190px / PPU 128 (제출 복사본에서 자동 교정)
- `SHELL_ForestCannon.cs`: 충돌 감지 및 상승→정점→하강 전체 포물선 유지
- `Cannon_Launch.wav`: 발사 효과음
- `SHELL_ForestCannon.prefab`: SpriteRenderer, Rigidbody2D, Trigger Collider2D, AudioSource, 동작 컴포넌트 연결

## 병합/테스트

- 기존 공용 Stage01, Player 프리팹은 수정하지 않았습니다.
- 기존 테스트 씬, 시소 에셋과 시소 코드, 에디터 자동 생성기는 PR에서 제외했습니다.
- 플레이어는 2D Collider와 활성 Rigidbody2D가 필요합니다.
- 팀 프로젝트의 플레이어 이동 구현에 따라 실제 발사 궤적은 Unity에서 확인해야 합니다.
- 고양이 크기와 착지 위치는 스테이지 설계에 맞게 조절하세요.
