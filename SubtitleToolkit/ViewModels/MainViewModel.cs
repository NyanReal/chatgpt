using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Win32;
using SubtitleToolkit.Models;
using SubtitleToolkit.Services;

namespace SubtitleToolkit.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SubRipParser _parser = new();
    private readonly SubtitleTimingService _timingService = new();
    private readonly SubtitleSplitter _splitter = new();

    private SubRipDocument? _document;
    private string? _sourcePath;
    private CueViewModel? _selectedCue;
    private double _charactersPerSecond = 15d;
    private int _minimumDurationMs = 600;
    private int _minimumGapMs = 50;
    private int _maxCharactersPerCue = 45;
    private int _maxCharactersPerWordSplit = 28;
    private string _statusMessage = "준비됨";

    public ObservableCollection<CueViewModel> Cues { get; } = new();
    public ObservableCollection<string> ShortCueDiagnostics { get; } = new();
    public ObservableCollection<string> LastSplitSummary { get; } = new();

    public ICommand LoadFileCommand { get; }
    public ICommand ReloadFileCommand { get; }
    public ICommand SaveFileCommand { get; }
    public ICommand StretchDurationsCommand { get; }
    public ICommand SplitCuesCommand { get; }
    public ICommand RefreshDiagnosticsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel()
    {
        LoadFileCommand = new RelayCommand(_ => LoadFile());
        ReloadFileCommand = new RelayCommand(_ => ReloadFile(), _ => File.Exists(SourcePath));
        SaveFileCommand = new RelayCommand(_ => SaveFile(), _ => _document != null && !string.IsNullOrWhiteSpace(SourcePath));
        StretchDurationsCommand = new RelayCommand(_ => StretchDurations(), _ => _document != null);
        SplitCuesCommand = new RelayCommand(_ => SplitCues(), _ => _document != null);
        RefreshDiagnosticsCommand = new RelayCommand(_ => RefreshDiagnostics(), _ => _document != null);
    }

    public string? SourcePath
    {
        get => _sourcePath;
        private set
        {
            if (_sourcePath != value)
            {
                _sourcePath = value;
                OnPropertyChanged();
                RaiseCanExecuteChanges();
            }
        }
    }

    public int CueCount => Cues.Count;

    public CueViewModel? SelectedCue
    {
        get => _selectedCue;
        set
        {
            if (_selectedCue != value)
            {
                _selectedCue = value;
                OnPropertyChanged();
            }
        }
    }

    public double CharactersPerSecond
    {
        get => _charactersPerSecond;
        set
        {
            if (value <= 0)
            {
                return;
            }

            if (Math.Abs(_charactersPerSecond - value) > double.Epsilon)
            {
                _charactersPerSecond = value;
                OnPropertyChanged();
            }
        }
    }

    public int MinimumDurationMs
    {
        get => _minimumDurationMs;
        set
        {
            if (value < 0)
            {
                return;
            }

            if (_minimumDurationMs != value)
            {
                _minimumDurationMs = value;
                OnPropertyChanged();
            }
        }
    }

    public int MinimumGapMs
    {
        get => _minimumGapMs;
        set
        {
            if (value < 0)
            {
                return;
            }

            if (_minimumGapMs != value)
            {
                _minimumGapMs = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxCharactersPerCue
    {
        get => _maxCharactersPerCue;
        set
        {
            if (value <= 0)
            {
                return;
            }

            if (_maxCharactersPerCue != value)
            {
                _maxCharactersPerCue = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxCharactersPerWordSplit
    {
        get => _maxCharactersPerWordSplit;
        set
        {
            if (value <= 0)
            {
                return;
            }

            if (_maxCharactersPerWordSplit != value)
            {
                _maxCharactersPerWordSplit = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public int ShortCueCount => ShortCueDiagnostics.Count;

    private void LoadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "자막 파일 (*.srt)|*.srt|모든 파일 (*.*)|*.*",
            Title = "SRT 파일 선택"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFromPath(dialog.FileName);
        }
    }

    private void ReloadFile()
    {
        if (!string.IsNullOrWhiteSpace(SourcePath) && File.Exists(SourcePath))
        {
            LoadFromPath(SourcePath);
        }
    }

    private void LoadFromPath(string filePath)
    {
        try
        {
            var text = File.ReadAllText(filePath, Encoding.UTF8);
            var document = _parser.Parse(text);
            _document = document;
            SourcePath = filePath;
            UpdateCueCollection(document);
            RefreshDiagnostics();
            StatusMessage = $"불러오기 완료: {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"불러오기 실패: {ex.Message}";
        }
    }

    private void SaveFile()
    {
        if (_document == null || string.IsNullOrWhiteSpace(SourcePath))
        {
            return;
        }

        try
        {
            var text = _parser.Serialize(_document);
            File.WriteAllText(SourcePath, text, Encoding.UTF8);
            StatusMessage = "저장 완료";
        }
        catch (Exception ex)
        {
            StatusMessage = $"저장 실패: {ex.Message}";
        }
    }

    private void StretchDurations()
    {
        if (_document == null)
        {
            return;
        }

        try
        {
            var adjusted = _timingService.Stretch(
                _document,
                CharactersPerSecond,
                TimeSpan.FromMilliseconds(MinimumDurationMs),
                TimeSpan.FromMilliseconds(MinimumGapMs));
            _document = adjusted;
            UpdateCueCollection(adjusted);
            RefreshDiagnostics();
            StatusMessage = "표시 시간을 조정했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"조정 실패: {ex.Message}";
        }
    }

    private void SplitCues()
    {
        if (_document == null)
        {
            return;
        }

        try
        {
            var (doc, summary) = _splitter.SplitDocument(_document, MaxCharactersPerCue, MaxCharactersPerWordSplit);
            _document = doc;
            UpdateCueCollection(doc);
            LastSplitSummary.Clear();
            foreach (var line in summary.Take(20))
            {
                LastSplitSummary.Add(line);
            }

            if (summary.Count > 20)
            {
                LastSplitSummary.Add($"…외 {summary.Count - 20}건");
            }

            RefreshDiagnostics();
            StatusMessage = summary.Count > 0 ? $"{summary.Count}개의 자막을 분할했습니다." : "분할할 자막이 없습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"분할 실패: {ex.Message}";
        }
    }

    private void RefreshDiagnostics()
    {
        if (_document == null)
        {
            ShortCueDiagnostics.Clear();
            OnPropertyChanged(nameof(ShortCueCount));
            return;
        }

        var diagnostics = _timingService.GetShortCueDiagnostics(
            _document,
            CharactersPerSecond,
            TimeSpan.FromMilliseconds(MinimumDurationMs));
        ShortCueDiagnostics.Clear();
        foreach (var item in diagnostics.Take(100))
        {
            ShortCueDiagnostics.Add(item);
        }

        if (diagnostics.Count > 100)
        {
            ShortCueDiagnostics.Add($"…외 {diagnostics.Count - 100}건");
        }

        OnPropertyChanged(nameof(ShortCueCount));
        StatusMessage = $"짧은 자막 {diagnostics.Count}건";
    }

    private void UpdateCueCollection(SubRipDocument document)
    {
        Cues.Clear();
        foreach (var cue in document.Cues)
        {
            Cues.Add(new CueViewModel(cue));
        }

        _selectedCue = null;
        OnPropertyChanged(nameof(SelectedCue));
        OnPropertyChanged(nameof(CueCount));
        RaiseCanExecuteChanges();
    }

    private void RaiseCanExecuteChanges()
    {
        if (LoadFileCommand is RelayCommand load)
        {
            load.RaiseCanExecuteChanged();
        }

        if (ReloadFileCommand is RelayCommand reload)
        {
            reload.RaiseCanExecuteChanged();
        }

        if (SaveFileCommand is RelayCommand save)
        {
            save.RaiseCanExecuteChanged();
        }

        if (StretchDurationsCommand is RelayCommand stretch)
        {
            stretch.RaiseCanExecuteChanged();
        }

        if (SplitCuesCommand is RelayCommand split)
        {
            split.RaiseCanExecuteChanged();
        }

        if (RefreshDiagnosticsCommand is RelayCommand refresh)
        {
            refresh.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
