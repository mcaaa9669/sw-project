SHELL 32px GRID / ADVANCED 3 GIMMICKS — 2026-11-18
==================================================
내부 스프라이트는 32x32의 2x2 타일 격자(64x64)로 제작했습니다.
각 타일은 Art/Tiles32에 정확히 32x32 PNG로 별도 보관됩니다.
Import Settings: Sprite / 32 Pixels Per Unit / Filter Mode Point / Compression None / Mip Maps OFF.

포함 콘텐츠:
- 무너지는 이끼 발판 : 온전함→금→균열→붕괴→복구 (Rigidbody2D의 접촉 시 작동)
- 버섯 점프패드 : 플레이어 물리 바디 접촉 시 튕김 및 4단계 애니메이션
- 덩굴 엘리베이터 : Kinematic Rigidbody2D로 이동, Activate()/Deactivate()/Toggle() 공개
- 기존 프로젝트 파일과 이름 충돌하지 않도록 독립 Assets/SHELL_AdvancedGimmicks_20261118 아래에 생성
- 3개 새 효과음도 포함
- 샘플 씬에서 더미 캐릭터 2개 및 자동 순환 엘리베이터 시험 가능

설치 방법 (Unity Play 중지 후):
1. ZIP을 ~/Documents/GitHub/sw-project 위치에 압축 해제
2. Unity 창으로 돌아가 스크립트 컴파일/에셋 임포트 완료를 기다림
3. Assets/SHELL_AdvancedGimmicks_20261118/Prefabs 아래 프리팹 3개 생성됨
4. Assets/SHELL_AdvancedGimmicks_20261118/Demo/SHELL_ThreeGimmicks_Showcase.unity 더블클릭 후 Play

주의:
- 실제 Stage01 씬, 기존 BushWall.prefab, 팀원 스크립트는 수정하지 않습니다.
- 기존 플레이어(태그/레이어/충돌 방식)와의 연동은 프로젝트에서 테스트해야 합니다.
- 엘리베이터는 기본적으로 멈춰 있으며, 레버 담당자는 UnityEvent 또는 스크립트에서
  GetComponent<SHELL_VineElevator>().Toggle()을 호출하면 됩니다.
- 에셋 및 스크립트를 테스트하지 않고 바로 공용 develop에 병합하지 마세요.
- 자동 생성 실패 시 Console 오류를 확인하고 Tools>SHELL>Create 3 Advanced Gimmicks (safe) 실행.
- 오디오 파일과 비주얼은 이 데모를 위해 새로 만든 것이며 별도 상용 음원은 포함하지 않습니다.
