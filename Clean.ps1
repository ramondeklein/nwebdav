Get-ChildItem -Recurse -Filter 'bin' -Directory | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Filter 'obj' -Directory | Remove-Item -Recurse -Force
