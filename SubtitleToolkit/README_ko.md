# Subtitle Toolkit

SubRip(SRT) 자막을 불러와 표시 시간 조정, 문장/어절 분할, 짧은 구간 진단을 제공하는 Windows용 WPF 응용 프로그램입니다.

## 주요 기능

- **파일 관리**: UTF-8 SRT 파일을 불러오기, 다시 불러오기, 저장.
- **표시 시간 확장**: 초당 글자 수·최소 표시 시간·최소 간격 기준으로 짧은 자막을 자동 연장.
- **문장/어절 분할**: 긴 자막을 문장 단위로 우선 분리하고, 불가피할 경우 어절 단위로 의미를 유지하며 분할.
- **진단 도구**: 설정한 읽기 속도 기준보다 짧게 표시되는 자막을 목록으로 확인.

## 시작하기

1. Windows 환경에서 .NET 8 SDK를 설치합니다.
2. 프로젝트 폴더에서 다음 명령을 실행합니다.
   ```bash
   dotnet restore
   dotnet build
   ```
3. Visual Studio 또는 `dotnet run`으로 WPF 앱을 실행합니다.
4. SRT 파일을 불러온 뒤 각 탭에서 표시 시간 조정, 분할, 진단을 수행합니다.

## 프로젝트 구성

- `Models/`: `SubRipCue`, `SubRipDocument` 등 자막 데이터 모델.
- `Services/`: 자막 파싱, 표시 시간 조정, 분할 로직.
- `ViewModels/`: `MainViewModel`과 커맨드, UI 바인딩용 뷰모델.
- `App.xaml`, `MainWindow.xaml`: 애플리케이션 엔트리와 UI 레이아웃.

## 제한 사항

- 초기 NuGet 패키지 복원을 위해 네트워크 연결이 필요합니다.
- WPF 특성상 Windows 이외 플랫폼에서는 실행할 수 없습니다.

## 라이선스

라이선스가 제공된 경우 `LICENSE` 파일을 참조하세요.
