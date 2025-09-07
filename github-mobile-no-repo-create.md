요점만!
	•	결론: GitHub 모바일 앱(iOS/Android)에서는 새 리포지터리를 직접 만들 수 없습니다.(포크·이슈/PR 관리 등은 가능) 따라서 브라우저/데스크톱/CLI 중 하나로 생성해야 해요.  ￼ ￼

대안 1) 모바일 브라우저(Safari/Chrome)에서 생성
	1.	github.com 로그인 → 우상단 ＋ → New repository.
	2.	Repository name, Public/Private, 필요하면 README / .gitignore / License 체크 → Create repository.  ￼
	•	바로 가기: 주소창에 https://github.com/new 입력해도 같은 화면으로 이동합니다.  ￼

대안 2) GitHub Desktop(맥/윈도우)
	•	**File → New repository…**에서 이름·경로 입력 → Create repository → 필요하면 Publish repository로 GitHub에 올리기.  ￼

대안 3) GitHub CLI(터미널)

gh auth login
gh repo create my-repo --private --clone

프롬프트에 따라 공개 범위/초기화 옵션을 선택하면 새 리포가 만들어집니다.  ￼ ￼