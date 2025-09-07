# 모바일 GitHub 앱에서 새 리포지터리 생성 불가

**TL;DR**: GitHub **모바일 앱(iOS/Android)** 에서는 새 리포지터리를 직접 만들 수 없습니다.  
대신 다음 대안을 사용하세요.

---

## 대안 1) 모바일 브라우저(Safari/Chrome)

1. `github.com`에 로그인합니다.
2. 우상단 **＋** 버튼 → **New repository**를 누릅니다.
3. **Repository name**, **Public/Private**를 선택하고 필요 시 **README / .gitignore / License**를 체크한 뒤 **Create repository**를 누릅니다.  
   - 바로 가기: 주소창에 `https://github.com/new` 입력

## 대안 2) GitHub Desktop(맥/윈도우)

1. **File → New repository…**에서 이름과 로컬 경로를 정하고 **Create repository**를 누릅니다.
2. 필요하면 **Publish repository**를 눌러 GitHub로 올립니다.

## 대안 3) GitHub CLI(터미널)

```bash
gh auth login
gh repo create my-repo --private --clone
```
프롬프트에 따라 공개 범위/초기화 옵션을 선택하면 새 리포지터리가 생성됩니다.

---

## README에 넣을 링크(복사해서 사용)

```md
[모바일 GitHub 앱: 새 리포지터리 생성 불가](./github-mobile-no-repo-create.md)
```

---

마지막 업데이트: 2025-09-07 (KST)