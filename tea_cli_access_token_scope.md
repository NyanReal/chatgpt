# tea CLI access token 오류 해결

`tea` CLI에서 아래 오류가 발생할 수 있습니다:

```
Error: access token must have a scope
```

이 오류는 **토큰에 필요한 권한(scope)이 지정되지 않았을 때** 발생합니다.

## 해결 방법

1. Personal Access Token 생성 (GitHub 기준)
   - GitHub → **Settings → Developer settings → Personal access tokens → Tokens (classic)**
   - `Generate new token` 클릭
   - 필요한 scope 체크:
     - `repo` (저장소 접근용)
     - `read:org` (조직 정보 필요시)
     - `gist` (gist 사용시)
   - 토큰 생성 후 복사

2. 토큰 tea에 등록
   ```bash
   tea login add
   ```
   이후 username, 토큰, 서버 주소 입력 (예: https://github.com)

   또는 환경 변수로 설정:
   ```bash
   export GITEA_TOKEN=your_token_here
   ```

3. scope 확인
   - scope 없는 토큰은 API 호출이 차단됩니다.
   - 부족하면 새로 생성해야 합니다.

---

> 참고: Gitea(자가호스팅) 서버 사용 시에는 해당 서버의 사용자 설정에서 토큰을 생성하고, API 접근에 필요한 scope를 반드시 부여해야 합니다.
