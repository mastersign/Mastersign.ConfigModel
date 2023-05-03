$ErrorActionPreference = 'Stop'

if ($Host.Version.Major -lt 7) {
    if (!(Get-Command pwsh)) {
        throw "PowerShell >= 7 required"
    } else {
        pwsh -ExecutionPolicy ByPass -NoLogo -File $MyInvocation.MyCommand.Definition @args
        exit $LASTEXITCODE
    }
}

if (!(Get-Command docker)) {
    throw "Could not find docker command"
}

function delay([int]$ms) {
    Write-Host "Sleeping $ms ms..."
    [System.Threading.Thread]::Sleep([TimeSpan]::FromMilliseconds($ms))
}

Push-Location "$PSScriptRoot\.."
try {

    $imageName = "local/mastersign-configmodel-test:latest"
    docker build --file "$PSScriptRoot\Dockerfile" --tag $imageName .
    if (!$?) { throw "Docker build failed with exit code $LASTEXITCODE" }

    $tmpDir = "$PSScriptRoot\tmp"
    if (Test-Path $tmpDir) { Remove-Item -Path $tmpDir -Recurse -Force }
    mkdir $tmpDir | Out-Null

    "Value: Initial`n" | Out-File "$tmpDir\model.yaml" -Encoding ASCII

    Write-Host "Starting test container"
    $containerId = docker run -d -v "${tmpDir}:/data" $imageName /data/model.yaml 10
    if (!$?) { throw "Running test container failed with exit code $LASTEXITCODE" }

    delay 1000
    "Value: Change 1" | Out-File "$tmpDir\model.yaml" -Encoding ASCII
    delay 4000
    docker run --rm -it -v "${tmpDir}:/data" busybox sh -c 'echo "Value: Change 2" > /data/model.yaml'
    delay 4000

    Write-Host "Copying output to tmp\log.txt"
    docker logs $containerId | Out-File "$tmpDir\log.txt" -Encoding utf8NoBOM
    Write-Host "Removing container"
    docker rm -f $containerId

    Write-Output ""
    Write-Output "---- OUTPUT ----"
    Get-Content "$tmpDir\log.txt" | Out-Default

    if (Test-Path "$tmpDir\changes.txt") {
        Write-Output ""
        Write-Output "---- CHANGES ----"
        Get-Content "$tmpDir\changes.txt"
    }

    if (Test-Path "$tmpDir\error.txt") {
        Write-Output ""
        Write-Output "---- ERROR ----"
        Get-Content "$tmpDir\error.txt"
    }

} finally {
    Pop-Location
}
