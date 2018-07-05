$version = '0.1.29'
$apiKey = Get-Content nuget.apikey
$folders = @("NWebDAV.Server", "NWebDAV.Server.AspNet", "NWebDav.Server.AspNetCore", "NWebDAV.Server.HttpListener")
ForEach ($folder in $folders) {
	Push-Location $folder
	& dotnet build -c Release
	& dotnet nuget push ./bin/Release/$folder.$version.nupkg -k $apiKey -s https://api.nuget.org/v3/index.json
	Pop-Location
}
