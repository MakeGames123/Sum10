# Sum10 개발 TODO

## 게임 플로우
- [ ] 튜토리얼 플로우 구현 (게임 시작 → 튜토리얼 → 첫판)
- [ ] 게임 시작 연출 구현
- [ ] 인게임 햅틱 및 하이스코어 갱신 연출 구현
- [ ] 게임 오버 패널 및 연출 구현

## UI 에셋 작업

### 5. 스킨 선택창 (Skin)
| 에셋명 | 설명 | 우선순위 | 완료 |
|--------|------|----------|------|
| Button_Arrow_Left | 좌측 화살표 | 높음 | ✅ |
| Button_Arrow_Right | 우측 화살표 | 높음 | ✅ |
| Button_Purchase | 가격 버튼 | 높음 | ✅ |
| Tag_Sale | 할인 태그 | 중간 | |
| Tag_Equipped | 장착중 태그 | 중간 | |
| Skin_Cat_01~09 | 고양이 스킨 세트 (최소 9개) | 높음 | |
| Button_Equip | 장착하기 | 높음 | |

### 6. 재화 상점 (Shop)
| 에셋명 | 설명 | 우선순위 | 완료 |
|--------|------|----------|------|
| Panel_ShopItem | 상점 아이템 카드 배경 | 높음 | ✅ |
| Tab | 탭 4개 (아이콘) | 높음 | 선물만 |
| Banner_Ad | 광고 할인 배너 | 중간 | |
| icon_Package | 패키지 아이콘 | 중간 | |
| icon_Video | 광고보상 아이콘 | 중간 | |
| icon_FirstPurchase | 첫구매 아이콘 | 중간 | |
| Diamond_Small | 다이아 x10 이미지 | 중간 | |
| Diamond_Medium | 다이아 x30 이미지 | 중간 | |
| Diamond_Large | 다이아 x100 이미지 | 중간 | |
| Diamond_Box | 다이아 상자 (큰 패키지용) | 중간 | |

### 7. 설정 (Settings)
| 에셋명 | 설명 | 우선순위 | 완료 |
|--------|------|----------|------|
| Slider_Track | 슬라이더 트랙 | 높음 | |
| Slider_Fill | 슬라이더 채우기 | 높음 | |
| Slider_Handle | 슬라이더 핸들 | 높음 | |
| icon_Music | 음악 아이콘 | 높음 | |
| icon_SFX | 효과음 아이콘 | 높음 | |
| icon_Vibrate | 진동 아이콘 | 중간 | |
| icon_Restore | 구매복구 아이콘 | 중간 | |
| icon_CustomerService | 고객지원 아이콘 | 중간 | |
| icon_Support | 도움말 아이콘 | 중간 | |
| Button_Language | 언어 선택 버튼 | 중간 | |

---

## 완료된 작업
- [x] Flip 선택 애니메이션 구현 (B 모드)
- [x] 숫자 텍스트 애니메이션 동기화
- [x] 테마별 폰트 색상 지원 (ThemeData SO)
- [x] 힌트 무한 루프 애니메이션
- [x] 힌트 경로 셀 제거 시 힌트 취소 버그 수정
- [x] AB 테스트 (T키로 Bounce/Flip 전환)
- [x] 시간 추가 규칙 변경 (0~30초: 3초, 30~90초: 2초, 90~150초: 1초, 150초~: 0.5초)
