# Git + Firebase 팀 설정 가이드 (방법 A)

Firebase Unity SDK 안의 바이너리(`FirebaseCppApp` 등)가 **파일당 100MB를 넘어** GitHub 일반 저장소에 올릴 수 없습니다.  
그래서 **`Assets/Firebase/` 폴더는 Git에 넣지 않고**, 팀원이 Unity에서 **같은 버전 SDK를 Import** 하는 방식을 사용합니다.

| Git에 포함 ✅ | Git에 포함하지 않음 ❌ |
|-------------|----------------------|
| `Assets/google-services.json` | `Assets/Firebase/` 전체 |
| `Assets/StreamingAssets/google-services-desktop.json` | `Assets/Plugins/iOS/Firebase/` |
| `Assets/Scripts/Core/Auth/` | `Assets/Plugins/tvOS/Firebase/` |
| `Assets/Scripts/Features/UI/Auth/` | `Assets/Editor Default Resources/Firebase/` |
| `Assets/ExternalDependencyManager/` | |
| `Assets/Docs/*.md` | |
| 로그인·스토리·UI 스크립트, 씬 등 | |

로그인/회원가입 **동작 설명**은 [`FirebaseAuthSetup_KO.md`](FirebaseAuthSetup_KO.md) 를 이어서 읽으세요.

---

## SDK 버전 (팀 전원 동일하게)

- **Firebase Unity SDK 13.10.0**
- Import 패키지: **Authentication** + **Cloud Firestore**
- Android 패키지명: `com.capstone.TheLastRune`
- Firebase 프로젝트 ID: `the-last-rune`

---

# 1. 담당자 (나) — GitHub Desktop으로 올리기

이미 PC에 Firebase SDK가 Import 되어 있다면, **로컬에는 `Assets/Firebase`가 그대로 있어도 됩니다.** Git만 추적하지 않으면 됩니다.

### 1-1. `.gitignore` 반영 확인

저장소 루트 `.gitignore`에 Firebase 관련 경로가 추가되어 있어야 합니다. (이미 적용됨)

### 1-2. GitHub Desktop에서 커밋

1. **100MB 경고 창**이 뜨면 → **Cancel** (Commit anyway 하지 않기)
2. 왼쪽 **Changes** 목록 확인  
   - `Assets/Firebase/...` 파일이 **목록에 없어야** 정상 (ignore 적용됨)  
   - 만약 **여전히 보이면** → 해당 항목 전부 체크 해제하거나, 아래 1-3 실행
3. **반드시 체크해서 커밋할 것** (예시):
   - `Assets/google-services.json` (+ `.meta`)
   - `Assets/StreamingAssets/` (google-services-desktop 등)
   - `Assets/Scripts/Core/Auth/`
   - `Assets/Scripts/Features/UI/Auth/`
   - `Assets/ExternalDependencyManager/`
   - `Assets/Docs/`
   - 메인 스토리·설정·인벤 UI 스크립트, `ProtoType_LTG.unity` 등 본인 작업분
4. Summary 예: `feat: Firebase 인증·메인 스토리 (SDK는 로컬 Import)`  
5. **Commit to `LTG`** → **Push origin**

### 1-3. (이전에 Firebase를 스테이징했을 때만) 캐시에서 제거

터미널에서 프로젝트 루트:

```powershell
cd "프로젝트 경로"
git rm -r --cached "Assets/Firebase" 2>$null
git rm -r --cached "Assets/Plugins/iOS/Firebase" 2>$null
git rm -r --cached "Assets/Plugins/tvOS/Firebase" 2>$null
git rm -r --cached "Assets/Editor Default Resources/Firebase" 2>$null
```

이후 GitHub Desktop에서 다시 커밋 (Firebase 항목 없음).

### 1-4. main에 반영

- **권장:** `LTG` Push → GitHub에서 **Pull Request** → `main` Merge  
- 팀에 공지: **pull 후 아래 「2. 팀원」 절차 필수**

### 1-5. Firebase Console에서 팀 초대

1. [Firebase Console](https://console.firebase.google.com/) → 프로젝트 **the-last-rune**  
2. ⚙ **프로젝트 설정** → **사용자 및 권한** → 팀원 Gmail **초대** (Editor 또는 Viewer)  
3. Authentication **Email/Password** 사용 설정, Firestore 규칙은 `FirebaseAuthSetup_KO.md` 참고  

`google-services.json`은 Git으로 공유되므로, 팀원이 **별도 json 다운로드는 보통 불필요**합니다.

### 1-6. 팀 채팅에 붙일 한 줄

```
main(또는 LTG) pull 받은 뒤 → Assets/Docs/TeamGitFirebaseSetup_KO.md 「팀원」 절차대로 
Firebase Unity SDK 13.10.0 (Auth + Firestore) Import → Force Resolve → Play 테스트.
```

---

# 2. 팀원 — clone/pull 후 할 일

Firebase를 **처음부터 설치하는 것이 아니라**, Git에는 **코드 + 설정 파일**만 있고 **SDK 폴더는 각자 Import** 합니다.

### 2-1. 저장소 받기

1. GitHub Desktop 또는 `git clone` / `git pull` 로 **최신 main**(또는 합의한 브랜치) 받기  
2. Unity Hub로 프로젝트 열기 (팀과 **같은 Unity 버전** 권장)  
3. 첫 열기 시 컴파일이 끝날 때까지 대기  

### 2-2. Firebase Unity SDK Import (필수, 1회)

1. 브라우저: [Firebase Unity SDK 다운로드](https://firebase.google.com/download/unity)  
2. **버전 13.10.0** 에 맞는 패키지에서 아래만 Import (프로젝트에 이미 있으면 건너뛰기):
   - **FirebaseAuthentication.unitypackage**
   - **FirebaseFirestore.unitypackage**
3. Unity: **Assets → Import Package → Custom Package** → 위 두 파일 순서대로 Import  
4. Import 후 프로젝트에 생기는 폴더 (로컬 전용, Git 없음):
   - `Assets/Firebase/`
   - `Assets/Plugins/iOS/Firebase/` 등  
5. **`Assets/google-services.json` 이 있는지 확인** (Git에서 내려옴 — 덮어쓰지 말 것)

> 이미 담당자와 **같은 13.10.0**을 Import 했다면 2-2는 생략 가능.

### 2-3. Android 설정

1. **File → Build Settings → Android**  
2. **Player Settings → Other Settings → Package Name**  
   - `com.capstone.TheLastRune` (Firebase와 동일해야 함)  
3. **Assets → External Dependency Manager → Android Resolver → Force Resolve**  

### 2-4. Play 테스트

1. 씬 `Assets/Scenes/ProtoType_LTG.unity` 열기  
2. Console에 `[FirebaseBootstrap] Firebase 초기화 완료` 확인  
3. 회원가입 → **이메일 인증** → 로그인 → 타이틀 → 게임 시작 → 스토리 → 스킵 → 무기/룬 선택  

UI·씬 연결 상세는 [`FirebaseAuthSetup_KO.md`](FirebaseAuthSetup_KO.md) 3~5단계.

### 2-5. 자주 하는 실수

| 증상 | 원인 | 해결 |
|------|------|------|
| `Firebase` 폴더 없음 / 초기화 실패 | SDK 미 Import | 2-2 다시 |
| 100MB push 오류 (팀원이 SDK 커밋 시도) | SDK를 Git에 올림 | 커밋 취소, `.gitignore` 유지 |
| Firestore permission denied | Console 규칙 | 담당자에게 규칙 적용 요청 또는 `FirebaseAuthSetup_KO.md` |
| 패키지명 불일치 | Player Settings | `com.capstone.TheLastRune` |

---

# 3. 요약

| 역할 | Git | Unity |
|------|-----|--------|
| **담당자** | Firebase **폴더 제외**하고 push, `google-services.json` 포함 | 로컬 SDK 유지, Console에서 팀 초대 |
| **팀원** | pull | SDK **13.10.0 Auth+Firestore** Import, Force Resolve, 문서대로 테스트 |

질문은 담당자에게: Firebase Console 초대 여부, pull 브랜치 이름(`main` / `LTG`).
