# Firebase 로그인/회원가입 — Unity 설정 가이드

이 프로젝트는 **아이디 + 비밀번호**로 로그인하고, **회원가입 시 이메일 인증**이 끝나야만 로그인할 수 있도록 구현되어 있습니다.

- 로그인: 아이디(또는 이메일) + 비밀번호  
- 회원가입: 아이디, 이메일, 비밀번호, 비밀번호 확인, 닉네임  
- 아이디는 Firestore `usernames` 컬렉션에 저장되며, 실제 Firebase Auth는 **이메일/비밀번호**로 동작합니다.

---

## 0단계: Git과 Firebase SDK (팀 필독)

`Assets/Firebase/` 는 **GitHub 용량 제한(100MB/파일)** 때문에 **Git에 올리지 않습니다.**

| 누가 | 무엇을 읽나 |
|------|-------------|
| **담당자 (push 하는 사람)** | [`TeamGitFirebaseSetup_KO.md`](TeamGitFirebaseSetup_KO.md) → **「1. 담당자」** |
| **팀원 (pull 받는 사람)** | [`TeamGitFirebaseSetup_KO.md`](TeamGitFirebaseSetup_KO.md) → **「2. 팀원」** → 이 문서 1단계~ |

팀원은 pull 후 **Firebase Unity SDK 13.10.0** 에서 **Authentication + Firestore** 를 Import 한 뒤, 아래 단계를 진행하세요.  
`Assets/google-services.json` 은 Git에 포함되어 있습니다.

---

## 1단계: Firebase Console 설정

1. 브라우저에서 [Firebase Console](https://console.firebase.google.com/) 접속  
2. 프로젝트 **the-last-rune** 선택 (또는 본인 프로젝트)  
3. 왼쪽 **Build → Authentication**  
4. **Sign-in method** 탭 → **Email/Password** → **사용 설정(Enable)** → 저장  
5. (선택) **Templates** 탭에서 인증 메일 제목/본문을 한국어로 수정 가능  

### Firestore 데이터베이스

1. **Build → Firestore Database**  
2. 아직 없으면 **데이터베이스 만들기** → 테스트 모드 또는 프로덕션 규칙 선택  
3. 아래 **보안 규칙**을 적용 (테스트용은 개발 중만 사용):

```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read: if request.auth != null && request.auth.uid == userId;
      allow create: if request.auth != null && request.auth.uid == userId;
      allow update: if request.auth != null && request.auth.uid == userId;
      allow delete: if request.auth != null && request.auth.uid == userId;
    }
    match /usernames/{username} {
      allow read: if true;
      allow create: if request.auth != null
        && request.resource.data.uid == request.auth.uid;
      allow update: if false;
      allow delete: if request.auth != null
        && resource.data.uid == request.auth.uid;
    }
  }
}
```

> `usernames`는 로그인 시 아이디→이메일 조회에 필요해서 **읽기는 공개**입니다. 운영 시에는 Cloud Functions로 조회하는 방식이 더 안전합니다.

---

## 2단계: Unity — google-services.json 확인

1. `Assets/google-services.json` 이 있는지 확인 (이미 있음)  
2. **File → Build Settings → Android** 선택 후 **Player Settings**  
3. **Other Settings → Package Name** 이 Firebase Android 앱 패키지명과 **완전히 동일**한지 확인  
4. 설정을 바꿨다면: **Assets → External Dependency Manager → Android Resolver → Force Resolve**  

---

## 3단계: 씬에 매니저 오브젝트 추가

메인 메뉴 씬(예: `ProtoType_LTG`)을 엽니다.

### 3-1. FirebaseBootstrap

1. Hierarchy 우클릭 → **Create Empty** → 이름 `FirebaseBootstrap`  
2. **Add Component** → `FirebaseBootstrap`  

### 3-2. AuthManager

1. **Create Empty** → 이름 `AuthManager`  
2. **Add Component** → `AuthManager`  

두 오브젝트는 씬 전환 시에도 유지하려면 `DontDestroyOnLoad`가 코드에 이미 적용되어 있습니다.

---

## 4단계: 로그인 화면 + GameStart 분리

**로그인 UI는 GameStart 밖, Canvas 직접 자식**으로 만듭니다.  
게임 실행 시 로그인만 보이고, 로그인 성공 후 **GameStart 전체**가 켜집니다.

### 목표 Hierarchy

```
Canvas
├── FirebaseBootstrap
├── AuthManager
├── AuthScreen                 ← AuthFlowController 붙임, 항상 활성
│   ├── PressAnyKeyPanel       ← 처음 보이는 화면 (아무 키나 누르시오)
│   ├── LoginPanel             ← 비활성
│   ├── SignUpPanel            ← 비활성
│   ├── ForgotPasswordPanel    ← 비활성
│   └── LoadingPanel           ← 비활성
└── GameStart                  ← Inspector 체크 해제(비활성)로 시작
    ├── Title
    └── ButtonStart
```

### 4-1. GameStart 끄기

1. Hierarchy에서 **GameStart** 선택  
2. Inspector 맨 위 **체크 해제** → 비활성  

### 4-2. AuthScreen 만들기

1. **Canvas** 우클릭 → **Create Empty** → 이름 `AuthScreen`  
2. Rect Transform: Stretch 전체 화면(Anchor Min 0,0 / Max 1,1, Offset 0)  

### 4-3. PressAnyKeyPanel (AuthScreen 자식, 첫 화면)

1. **AuthScreen** 우클릭 → **UI → Panel** → `PressAnyKeyPanel`  
2. 자식 **TMP_Text**: `PressAnyKeyText` — 문구 예: `아무 키나 누르시오...`  
3. **LoginPanel / SignUpPanel / ForgotPasswordPanel** 은 **비활성**으로 두고, **PressAnyKeyPanel만 활성**으로 시작  
4. 키·마우스·터치·패드 입력 시 → 로딩 → **LoginPanel** 로 전환 (`AuthFlowController`가 처리)

### 4-4. StatusText (AuthScreen 직계 자식, 공용)

1. **AuthScreen** 우클릭 → **UI → Text - TextMeshPro** → `StatusText`  
2. 로그인·회원가입·비밀번호 찾기 **모든** 상태 메시지를 여기에 표시합니다.  
3. **SignUpPanel / LoginPanel 안에는 StatusText 를 두지 않습니다** (있으면 삭제).

### 4-5. LoginPanel (AuthScreen 자식, 비활성)

1. **AuthScreen** 우클릭 → **UI → Panel** → `LoginPanel` → **체크 해제**  
2. 자식 추가:
   - **TMP_InputField**: `LoginIdInput`, `LoginPasswordInput` (Password 타입)  
   - **Button**: `LoginButton`(로그인), `GoSignUpButton`(회원가입), `ForgotPasswordButton`(비밀번호 찾기), `QuitButton`(끝내기)  

### 4-6. SignUpPanel (AuthScreen 자식, 비활성)

1. **UI → Panel** → `SignUpPanel` → **체크 해제**  
2. 입력 5개 + `SignUpButton`, `BackToLoginButton`  

### 4-7. ForgotPasswordPanel (AuthScreen 자식, 비활성)

1. **UI → Panel** → `ForgotPasswordPanel` → **체크 해제**  
2. **TMP_InputField**: `ForgotPasswordInput` (아이디 또는 이메일)  
3. **Button**: `SendResetEmailButton`, `ForgotBackToLoginButton`  

### 4-8. AuthFlowController 연결

1. **AuthScreen** 선택 → **Add Component** → `AuthFlowController`  
2. Inspector 연결:

| 필드 | 연결 대상 |
|------|-----------|
| Game Start Root | **GameStart** (타이틀 전체) |
| Auth Screen Root | **AuthScreen** |
| Press Any Key Panel | PressAnyKeyPanel |
| Press Any Key Text | PressAnyKeyText (선택) |
| Login Panel | LoginPanel |
| Sign Up Panel | SignUpPanel |
| Forgot Password Panel | ForgotPasswordPanel |
| Login Id Input | LoginIdInput |
| Login Password Input | LoginPasswordInput |
| Login Button | LoginButton |
| Go To Sign Up Button | GoSignUpButton |
| Forgot Password Button | ForgotPasswordButton |
| Quit Button | QuitButton |
| (회원가입 필드들) | ... |
| Forgot Password Id Or Email Input | ForgotPasswordInput |
| Send Reset Email Button | SendResetEmailButton |
| Forgot Back To Login Button | ForgotBackToLoginButton |
| Status Text | AuthScreen 직계 자식 **StatusText** |
| Logout Button | (선택) |

3. **Play** 후 Console에 `[FirebaseBootstrap] Firebase 초기화 완료` 가 보이는지 확인  

---

## 5단계: 동작 테스트 순서

### 회원가입

1. **회원가입** 버튼 → 정보 입력 → **가입**  
2. 메시지: "인증 메일을 보냈습니다..."  
3. **실제 이메일** 수신함 확인 (스팸함 포함)  
4. 메일의 **이메일 인증** 링크 클릭  

### 로그인

1. 인증 **전** 로그인 시도 → "이메일 인증이 완료되지 않았습니다"  
2. 인증 **후** 아이디 + 비밀번호 로그인 → 성공 시 **게임 시작** 버튼 표시  

### 인증 메일 재전송

- 로그인 화면에서 아이디/비밀번호 입력 후 **인증 메일 재전송** 버튼  

---

## 6단계: 자주 나는 문제

| 증상 | 해결 |
|------|------|
| Firebase 초기화 실패 | Android/iOS 빌드 타겟, `google-services.json`, 패키지명 확인 |
| 이메일이 안 옴 | Firebase Console → Authentication → Templates, 스팸함, 이메일 오타 |
| 아이디를 찾을 수 없음 | Firestore `usernames` 문서 생성 여부, 회원가입 완료 여부 |
| Firestore permission denied | 위 보안 규칙 적용, Auth 로그인 상태 확인 |
| 회원탈퇴 실패 (권한) | `users`·`usernames` delete 규칙 적용 후 규칙 **게시** |
| 회원탈퇴 실패 (비밀번호) | 비밀번호 재입력; 오래된 세션은 "다시 로그인" 메시지 |
| Editor에서만 테스트 | **실제 기기 빌드** 권장 (일부 Firebase 기능은 Editor 제한) |

---

## 7단계: 설정 화면 — 회원 탈퇴 UI (Unity)

설정 패널(`SettingPanel` 등)에 **회원탈퇴** 버튼과 확인 팝업을 추가합니다.  
코드는 `SettingsUI` + `AuthFlowController.RequestDeleteAccount` 로 연결되어 있습니다.

### Hierarchy 예시 (SettingPanel 자식)

```
SettingPanel          ← SettingsUI 컴포넌트
├── ... (기존 슬라이더/드롭다운)
├── DeleteAccountButton    ← 버튼 텍스트: "회원탈퇴" (이름은 자유, 아래 이름이면 자동 연결)
└── DeleteAccountPanel     ← 처음 비활성(체크 해제)
    ├── 안내 TMP_Text (선택)
    ├── DeleteAccountPasswordInput   ← TMP_InputField, Content Type: Password
    ├── DeleteAccountMessageText     ← TMP_Text (오류/안내)
    ├── DeleteAccountConfirmButton   ← "확인"
    └── DeleteAccountCancelButton    ← "취소"
```

### Unity에서 할 일

1. **SettingPanel** 선택 → `SettingsUI` Inspector 확인  
2. 하단에 **Button** 추가 → 텍스트 `회원탈퇴` → 이름 `DeleteAccountButton` (선택)  
3. **Panel** 자식으로 `DeleteAccountPanel` 생성 → **비활성**으로 시작  
4. 패널 안에 비밀번호 **TMP_InputField** (`DeleteAccountPasswordInput`, Password)  
5. **확인** / **취소** 버튼, 안내용 **TMP_Text** (`DeleteAccountMessageText`)  
6. `SettingsUI` Inspector에 드래그 연결 (비워 두면 이름으로 자동 탐색 시도):
   - Delete Account Button  
   - Delete Account Panel  
   - Delete Account Password Input  
   - Delete Account Confirm / Cancel Button  
   - Delete Account Message Text  
7. **Play** → 설정 열기 → 회원탈퇴 → 비밀번호 입력 → 확인  
   - Firebase는 **최근 로그인**이 필요해 비밀번호 재입력이 필수입니다.  
   - 성공 시 로그인 화면으로 돌아갑니다.

### Firebase Console (탈퇴 시 필수)

1. 위 **Firestore 보안 규칙**에 `users` / `usernames` **delete** 권한이 있어야 합니다.  
2. 규칙 미적용 시 Console에 `permission denied` → `AuthManager` 가 한국어 안내 메시지를 표시합니다.  
3. Authentication에서 해당 사용자 문서는 `user.DeleteAsync()` 로 삭제됩니다.

---

## 8단계: (선택) iOS 빌드

1. Firebase Console에 iOS 앱 추가  
2. `GoogleService-Info.plist` 다운로드 → `Assets/` 에 넣기  
3. iOS Resolver 실행  

---

## 스크립트 위치

- `Assets/Scripts/Core/Auth/FirebaseBootstrap.cs`  
- `Assets/Scripts/Core/Auth/AuthManager.cs`  
- `Assets/Scripts/Core/Auth/UserProfileRepository.cs`  
- `Assets/Scripts/Core/Auth/AuthValidation.cs`  
- `Assets/Scripts/Features/UI/Auth/AuthFlowController.cs`  
- `Assets/Scripts/Features/UI/Auth/AuthInputUtility.cs`  
- `Assets/Scripts/Features/UI/SettingsUI.cs` (회원 탈퇴 UI)  

---

## 참고: 아이디 vs 이메일

Firebase Authentication은 기본적으로 **이메일**이 계정 ID입니다.  
본 프로젝트는 **표시용 아이디**를 Firestore에 저장하고, 로그인 시 아이디로 이메일을 찾아 로그인합니다.  
이메일 주소를 직접 입력해도 로그인됩니다 (`@` 포함 시 이메일로 처리).
