<#
.SYNOPSIS
  Build game 12 Con Giáp thành bản Windows chạy độc lập rồi mở lên chơi thử — không cần mở Unity Editor.

.DESCRIPTION
  Gọi Unity ở chế độ ẩn (batchmode) để xuất Builds\Windows\12ConGiap.exe, sau đó mở game trong cửa sổ riêng.
  Unity Editor phải đang TẮT (Unity không cho hai tiến trình mở cùng một project).
  Lần đầu (chưa có scene) script tự dựng scene trước khi build.

.PARAMETER Rebuild
  Dựng lại toàn bộ scene/prefab/nhân vật (Tools > Zodiac > Dựng prototype) trước khi build — dùng sau khi sửa code builder,
  bản đồ hay chỉ số nhân vật.

.PARAMETER SkipBuild
  Không build, chỉ mở bản đã build lần trước.

.PARAMETER NoRun
  Chỉ build, không mở game.

.EXAMPLE
  .\tools\choi-thu.ps1             # build rồi chơi
  .\tools\choi-thu.ps1 -Rebuild    # dựng lại scene, build rồi chơi
  .\tools\choi-thu.ps1 -SkipBuild  # mở lại bản cũ
#>
param(
    [switch]$Rebuild,
    [switch]$SkipBuild,
    [switch]$NoRun
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'Builds\Windows\12ConGiap.exe'
$log = Join-Path $root 'Logs\build-windows.log'

function Say($text, $color = 'Gray') { Write-Host $text -ForegroundColor $color }

if (-not $SkipBuild) {
    # ── Tìm Unity đúng phiên bản của project ──
    $versionLine = Select-String -Path (Join-Path $root 'ProjectSettings\ProjectVersion.txt') -Pattern '^m_EditorVersion:\s*(.+)$'
    $version = $versionLine.Matches[0].Groups[1].Value.Trim()
    $unity = $env:UNITY_PATH
    if (-not $unity) { $unity = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
    if (-not (Test-Path $unity)) {
        Say "Không tìm thấy Unity $version tại: $unity" 'Red'
        Say 'Cài đúng phiên bản qua Unity Hub, hoặc đặt biến môi trường UNITY_PATH trỏ tới Unity.exe.' 'Yellow'
        exit 1
    }

    # ── Unity Editor đang mở project này thì không build được ──
    $lock = Join-Path $root 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try { $stream = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None'); $stream.Close() }
        catch {
            Say 'Unity Editor đang mở project này — hãy tắt Unity rồi chạy lại.' 'Red'
            Say '(Hoặc trong Unity: Tools > Zodiac > Xuất bản chơi thử (Windows).)' 'Yellow'
            exit 1
        }
    }

    $needScenes = -not (Test-Path (Join-Path $root 'Assets\Zodiac\Scenes\Zodiac_Level03.unity'))
    $unityArgs = @('-batchmode', '-projectPath', "`"$root`"", '-executeMethod', 'ZodiacBuildPlayer.BuildWindowsBatch', '-logFile', "`"$log`"")
    if ($Rebuild -or $needScenes) {
        $unityArgs += '-zodiacRebuild'
        Say 'Dựng lại scene + build bản Windows (lần đầu có thể mất 5–15 phút)...' 'Cyan'
    } else {
        Say 'Đang build bản Windows (thường 1–5 phút)...' 'Cyan'
    }

    if (Test-Path $log) { Remove-Item $log -Force }
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -WindowStyle Hidden

    # ── In tiến độ trong lúc chờ ──
    $lastShown = ''
    while (-not $proc.HasExited) {
        Start-Sleep -Seconds 3
        if (Test-Path $log) {
            $line = Get-Content $log -Tail 400 -ErrorAction SilentlyContinue |
                Where-Object { $_ -match '\[ZodiacBuild\]|Compiling|Importing|Building|Packaging|DisplayProgressbar|Đã dựng' } |
                Select-Object -Last 1
            if ($line -and $line -ne $lastShown) {
                $lastShown = $line
                $short = if ($line.Length -gt 110) { $line.Substring(0, 110) + '…' } else { $line }
                Say ("  [{0:mm\:ss}] {1}" -f $watch.Elapsed, $short.Trim())
            }
        }
    }
    $proc.WaitForExit()

    if ($proc.ExitCode -ne 0 -or -not (Test-Path $exe)) {
        Say ("Build THẤT BẠI sau {0:mm\:ss} (mã {1}). Các dòng lỗi trong log:" -f $watch.Elapsed, $proc.ExitCode) 'Red'
        if (Test-Path $log) {
            Get-Content $log | Where-Object { $_ -match 'error CS|\[ZodiacBuild\]|Exception|Error building|Build Finished' } |
                Select-Object -Last 25 | ForEach-Object { Say "  $_" 'Yellow' }
            Say "Log đầy đủ: $log" 'Gray'
        }
        exit 1
    }
    Say ("Build xong sau {0:mm\:ss}: {1}" -f $watch.Elapsed, $exe) 'Green'
}

if ($NoRun) { exit 0 }
if (-not (Test-Path $exe)) {
    Say "Chưa có bản build: $exe — chạy lại không kèm -SkipBuild." 'Red'
    exit 1
}
Say 'Mở game (cửa sổ 1600×900, Alt+Enter: toàn màn hình)...' 'Cyan'
Start-Process -FilePath $exe -ArgumentList '-screen-fullscreen', '0', '-screen-width', '1600', '-screen-height', '900'
