$version = '0.1.32'
$apiKey = Get-Content nuget.apikey
$folders = @("NWebDav.Server", "NWebDav.Server.AspNet", "NWebDav.Server.AspNetCore", "NWebDav.Server.HttpListener")
ForEach ($folder in $folders) {
	Push-Location $folder
	& dotnet build -c Release
	& dotnet nuget push ./bin/Release/$folder.$version.nupkg -k $apiKey -s https://api.nuget.org/v3/index.json
	Pop-Location
}
