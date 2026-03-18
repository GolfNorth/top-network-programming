$root = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
$slnx = Get-ChildItem -Path $root -Filter '*.slnx' | Select-Object -First 1
$projects = if ($slnx) {
    ([xml](Get-Content $slnx.FullName)).Solution.Project.Path |
        ForEach-Object { ($_ -split '/')[0] }
} else {
    Get-ChildItem -Path $PSScriptRoot -Filter '*.csproj' -Recurse |
        ForEach-Object { $_.Directory.Name }
}

$checked = [bool[]]::new($projects.Count)
$cursor  = 0

function Redraw {
    [Console]::SetCursorPosition(0, 0)
    Write-Host "  Стрелки — навигация, Пробел — вкл/выкл, Enter — запустить, Esc — выход`n" -ForegroundColor DarkGray
    for ($i = 0; $i -lt $projects.Length; $i++) {
        $box = if ($checked[$i]) { '[*]' } else { '[ ]' }
        if ($i -eq $cursor) {
            Write-Host "> $box $($projects[$i])" -ForegroundColor Cyan
        } else {
            Write-Host "  $box $($projects[$i])"
        }
    }
    Write-Host ''
}

[Console]::CursorVisible = $false
Clear-Host

while ($true) {
    Redraw
    $key = [Console]::ReadKey($true)
    switch ($key.Key) {
        'UpArrow'   { if ($cursor -gt 0) { $cursor-- } }
        'DownArrow' { if ($cursor -lt $projects.Length - 1) { $cursor++ } }
        'Spacebar'  { $checked[$cursor] = -not $checked[$cursor] }
        'Enter' {
            $toRun = (0..($projects.Length - 1)) |
                     Where-Object  { $checked[$_] } |
                     ForEach-Object { $projects[$_] }
            if ($toRun) {
                [Console]::CursorVisible = $true
                Clear-Host
                foreach ($p in $toRun) {
                    Start-Process 'cmd' -ArgumentList "/k dotnet run --project $p" `
                                        -WorkingDirectory $root
                }
                exit
            }
        }
        'Escape' {
            [Console]::CursorVisible = $true
            Clear-Host
            exit
        }
    }
}
