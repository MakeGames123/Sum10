# Unity UI 개발 매뉴얼

## 1. UI 계층 구조 규칙

### 핵심 원칙: 빈 오브젝트를 부모로 사용

**패널의 메인 이미지가 최상위 부모가 되면 안됩니다.**

#### 이유
- 추후에 스케일이나 가로/세로 길이 조정할 때 달린 자식들을 전부 조정해야 하는 문제 발생
- 부모 Panel은 **빈 오브젝트**여야 함

#### 올바른 구조 예시 (SettingPanel)
```
SettingPanel (빈 오브젝트) ← 여기에 패널 관리 스크립트 부착
├── Panel (Image) ← 실제 패널 이미지
│   ├── BackShadow
│   ├── Background
│   ├── PauseText
│   ├── SoundSlider
│   ├── MusicVolume
│   ├── LobbyButton
│   ├── ContinueButton
│   └── Scroll View
```

---

## 2. 위치 조정 방법

### 기본 원칙
- 적응형(반응형) 적용 전까지 **전부 인스펙터로 조정**
- 코드로 안 하고 인스펙터에서 수동으로 배치

### 조정 기준
| 상황 | 방법 |
|------|------|
| 자식의 규모가 **작을** 경우 | **부모의 위치**로 조정 |
| 자식의 규모가 **클** 경우 | **자식들끼리** 개별 조정 |

---

## 3. 버튼 이벤트 처리

### Button 컴포넌트
- **사용함** - 간단한 클릭 구현에는 Button 컴포넌트 사용

### onClick 이벤트 할당
- **인스펙터에서 할당 안함**
- **코드에서 할당함**

#### 이유
- 인스펙터에서 이벤트 할당하면 추적이 어려움

#### 코드 작성 규칙
- 코드로 할당하고 **함수 옆에 주석으로 표시**해야 함

```csharp
// 예시
public class SomePanel : MonoBehaviour
{
    [SerializeField] private Button retryButton;

    private void Start()
    {
        retryButton.onClick.AddListener(OnRetryButtonClicked); // RetryButton onClick
    }

    private void OnRetryButtonClicked()
    {
        // 버튼 클릭 처리
    }
}
```

---

## 4. UI 관리 구조

### UIController 역할
- UI 전부를 조정하되 **패널 단위**, **기능 단위**로 관리

### 기능별 분리 원칙
- 관련 기능 코드는 해당 오브젝트에 부착
- 예: 시간 관련 UI 기능 코드는 **Timer 오브젝트**에 붙어있어야 함

```
Canvas
├── UIController (전체 UI 관리)
├── Timer (시간 관련 UI 기능)
├── ScorePanel (점수 관련 UI 기능)
└── SettingPanel (설정 관련 UI 기능)
```

---

## 5. 서버 연동

### 작업 순서
1. **UI 먼저 다 만들기**
2. **그 다음 서버랑 연결**

### 서버 연동이 필요한 부분
- 랭킹 페이지 등 까다로운 부분은 서버 코드랑 연동 필요

---

## 6. 권장 UI 계층 구조

```
Canvas
├── MainUIRoot (빈 오브젝트)
│   ├── LobbyPanel (빈 오브젝트)
│   │   ├── Header (빈 오브젝트)
│   │   │   └── ... (로고, 재화 표시 등)
│   │   └── Content (빈 오브젝트)
│   │       └── ... (로비 컨텐츠)
│   │
│   ├── RankingPanel (빈 오브젝트)
│   │   ├── Header (빈 오브젝트)
│   │   │   └── Panel_TitleBanner (Image)
│   │   ├── PodiumArea (빈 오브젝트)
│   │   │   └── ... (포디움 1,2,3위)
│   │   └── RankingList (빈 오브젝트)
│   │       └── ... (랭킹 Row들)
│   │
│   └── BottomNavBar (빈 오브젝트)
│       ├── NavBar_BG (Image)
│       └── ButtonGroup (빈 오브젝트)
│           ├── NavBtn_Shop
│           ├── NavBtn_Home
│           ├── NavBtn_Skin
│           └── NavBtn_Ranking
```

---

## 요약 체크리스트

- [ ] 패널 최상위는 빈 오브젝트인가?
- [ ] 이미지는 빈 오브젝트의 자식인가?
- [ ] 버튼 이벤트는 코드로 할당했는가?
- [ ] 할당한 이벤트에 주석을 달았는가?
- [ ] 기능별로 스크립트가 분리되어 있는가?
