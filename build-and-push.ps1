[System.Xml.XmlDocument]$file = New-Object System.Xml.XmlDocument
$file.load("$pwd/Directory.build.props")
$version = ($file.SelectNodes("/Project/PropertyGroup/VersionPrefix/text()")).Value

$apiKey = Get-Content nuget.apikey
$folders = @("NWebDav.Server", "NWebDav.Server.AspNet", "NWebDav.Server.AspNetCore", "NWebDav.Server.HttpListener")
ForEach ($folder in $folders) {
	Push-Location $folder
	& dotnet build -c Release
	& dotnet nuget push ./bin/Release/$folder.$version.nupkg -k $apiKey -s https://api.nuget.org/v3/index.json
	Pop-Location
}
