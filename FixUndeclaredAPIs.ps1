$numTargetFrameworks = 5
$command = "dotnet"
$arguments = @("format", "analyzers", "./NexusMods.Archives.Nx.sln", "--diagnostics", "RS0037", "RS0016")

for ($i = 1; $i -le $numTargetFrameworks; $i++)
{
     Write-Host "Executing command: $command $($arguments -join ' ') (framework $i)"
     & $command $arguments
}