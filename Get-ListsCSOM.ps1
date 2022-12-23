$url = "https://groverale.sharepoint.com/sites/toomanylists "

Connect-PnPOnline -Url $url -Interactive

$ctx = Get-PnPContext

$web = $ctx.Web
$ctx.Load($web)
Invoke-PnPQuery


$getListQuery = New-Object Microsoft.SharePoint.Client.GetListsParameters
$getListQuery.RowLimit = 500

## Will start at null or 0
$position = $getListQuery.ListCollectionPosition

do
{
    $getListQuery.ListCollectionPosition = $position
    $listCol = $web.GetLists($getListQuery)
    $ctx.Load($listCol)
    Invoke-PnPQuery
    $position = $listCol.ListCollectionPosition;
    $allLists += $listCol
    $allLists.Count

    ## Subsequent pages
    #$listCol | % { "Title: $($_.Title) ID: $($_.Id)"  }

} while ($null -ne $position)

## test
$uniqueLists = $allLists | Select-Object -Property Id -Unique

$uniqueLists.Count

## May fail on sites with 60k+ lists
$pnpLists = Get-PnPList

if (($allLists.Count -eq $uniqueLists.Count) -and ($allLists.Count -eq $pnpLists.Count))
{
    "Passed"
}