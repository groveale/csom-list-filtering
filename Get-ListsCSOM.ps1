$url = "https://groverale.sharepoint.com/sites/toomanylists "

Connect-PnPOnline -Url $url -Interactive

$ctx = Get-PnPContext

$lists = $ctx.Web.Lists

$ctx.Load($lists)
Invoke-PnPQuery

#$lists

Write-Host ""
Write-Host "Found $($lists.count) lists in site"
Write-Host ""