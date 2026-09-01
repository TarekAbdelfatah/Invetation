# Partner Department HTTP Integration Tests (Windows PowerShell 5.1 friendly)

Add-Type -AssemblyName System.Web

$ErrorActionPreference = 'Stop'
$BaseUrl = 'http://localhost:5272'

$results = New-Object System.Collections.Generic.List[object]
function Record {
    param([string]$Name, [string]$Expected, [string]$Actual, [string]$Detail = '')
    $ok = ($Expected -eq $Actual)
    $results.Add([pscustomobject]@{
        Name = $Name; Expected = $Expected; Actual = $Actual; Detail = $Detail; Pass = $ok
    }) | Out-Null
    $color = if ($ok) { 'Green' } else { 'Red' }
    $tag = if ($ok) { 'PASS' } else { 'FAIL' }
    Write-Host ("[{0}] {1} - expected {2}, actual {3}" -f $tag, $Name, $Expected, $Actual) -ForegroundColor $color
    if ($Detail) { Write-Host ("       " + $Detail) -ForegroundColor DarkGray }
}

function New-Session {
    return [pscustomobject]@{
        CookieJar = New-Object System.Net.CookieContainer
    }
}

function Get-Page {
    param($Session, [string]$Path)
    $req = [System.Net.HttpWebRequest]::Create(($BaseUrl + $Path))
    $req.Method = 'GET'
    $req.UserAgent = 'PartnerTest/1.0'
    $req.CookieContainer = $Session.CookieJar
    $req.AllowAutoRedirect = $false
    $resp = $req.GetResponse()
    $stream = $resp.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $body = $reader.ReadToEnd()
    $reader.Close()
    $resp.Close()
    return $body
}

function Post-Page {
    param($Session, [string]$Path, [string]$Body)
    $req = [System.Net.HttpWebRequest]::Create(($BaseUrl + $Path))
    $req.Method = 'POST'
    $req.UserAgent = 'PartnerTest/1.0'
    $req.ContentType = 'application/x-www-form-urlencoded'
    $req.CookieContainer = $Session.CookieJar
    $req.AllowAutoRedirect = $false
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Body)
    $req.ContentLength = $bytes.Length
    $reqStream = $req.GetRequestStream()
    $reqStream.Write($bytes, 0, $bytes.Length)
    $reqStream.Close()
    try {
        $resp = $req.GetResponse()
        $stream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $bodyResp = $reader.ReadToEnd()
        $reader.Close()
        $resp.Close()
        return @{ Status = [int]$resp.StatusCode; Body = $bodyResp; Location = $resp.Headers['Location'] }
    } catch [System.Net.WebException] {
        $resp = $_.Exception.Response
        $stream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $bodyResp = $reader.ReadToEnd()
        $reader.Close()
        $resp.Close()
        return @{ Status = [int]$resp.StatusCode; Body = $bodyResp; Location = $resp.Headers['Location'] }
    }
}

function Get-AntiforgeryToken {
    param($Session, [string]$Path)
    $html = Get-Page $Session $Path
    if ($html -match 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') {
        return $matches[1]
    }
    throw ("Antiforgery token not found on " + $Path)
}

function Login-As {
    param($Session, [string]$Username, [string]$Password)
    $token = Get-AntiforgeryToken $Session '/Account/Login'
    $body = 'username=' + [System.Web.HttpUtility]::UrlEncode($Username) +
            '&password=' + [System.Web.HttpUtility]::UrlEncode($Password) +
            '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($token)
    $r = Post-Page $Session '/Account/Login' $body
    Write-Host ("Login [{0}] => HTTP {1} {2}" -f $Username, $r.Status, $r.Location)
    if ($r.Status -ne 302) { throw ("Login failed for " + $Username) }
}

Write-Host "================================================================"
Write-Host "PARTNER DEPARTMENT ADVISORY SCORING - INTEGRATION TEST SUITE"
Write-Host "================================================================"

Write-Host ""
Write-Host "STEP 1: PARTNER LOGIN + INDEX DASHBOARD"
Write-Host "--------------------------------------------------------------------"
$partner = New-Session
Login-As $partner 'partner' 'Ibtikar@2026'

$dashboard = Get-Page $partner '/PartnerDashboard'
Record "Partner dashboard returns HTML"     "yes" $(if ($dashboard.Length -gt 0) {'yes'} else {'no'}) ("page size=" + $dashboard.Length)

Write-Host ""
Write-Host "STEP 2: SPECIALIZED LOGIN + CREATE PARTNER ASSIGNMENT"
Write-Host "--------------------------------------------------------------------"
$specialized = New-Session
Login-As $specialized 'specialized' 'Ibtikar@2026'

$ideasPage = Get-Page $specialized '/SpecializedDashboard/Referrals'
$ideaMatch = ($ideasPage | Select-String -Pattern 'href="/SpecializedDashboard/Details/([0-9a-f-]{36})"' -AllMatches | Select-Object -First 1)
if (-not $ideaMatch) { Write-Host "No ideas in specialized referrals - run DatabaseSeeder.SeedSampleIdeas first"; exit 0 }
$ideaHref = $ideaMatch.Matches[0].Groups[1].Value
Write-Host ("First idea: " + $ideaHref)

$reqToken = Get-AntiforgeryToken $specialized ("/SpecializedDashboard/Request/" + $ideaHref)
$reqPage = Get-Page $specialized ("/SpecializedDashboard/Request/" + $ideaHref)
$partnerIds = @($reqPage | Select-String -Pattern 'name="partnerIds"\s+value="([0-9a-f-]{36})"' -AllMatches | ForEach-Object { $_.Matches[0].Groups[1].Value })
Write-Host ("Available partner ids: " + ($partnerIds -join ', '))

if ($partnerIds.Count -gt 0) {
    $selectedPartner = $partnerIds[0]
    $reqBody = 'ideaId=' + $ideaHref +
               '&partnerIds=' + $selectedPartner +
               '&note=Test+note' +
               '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($reqToken)
    $r = Post-Page $specialized '/SpecializedDashboard/Request' $reqBody
    Record "Specialized creates partner assignment" "302" ([string]$r.Status) ("location=" + $r.Location)
}

$dashboard2 = Get-Page $partner '/PartnerDashboard'
$rows = @($dashboard2 | Select-String -Pattern 'href="/PartnerDashboard/Details/([0-9a-f-]{36})"' -AllMatches).Count
Write-Host ("Partner inbox now has " + $rows + " assignment row(s)")
Record "Partner sees the new assignment in inbox" "1+" ([string]$rows)

$assignmentId = ($dashboard2 | Select-String -Pattern 'href="/PartnerDashboard/Details/([0-9a-f-]{36})"' -AllMatches | Select-Object -First 1).Matches[0].Groups[1].Value
Write-Host ("Assignment id: " + $assignmentId)

Write-Host ""
Write-Host "STEP 3: PARTNER DETAILS - SPECIALIZED SECTION + RETURN-ONLY VALIDATION"
Write-Host "--------------------------------------------------------------------"
$details = Get-Page $partner ("/PartnerDashboard/Details/" + $assignmentId)
Record "Details returns 200 HTML"           "yes" $(if ($details.Length -gt 0) {'yes'} else {'no'}) ("size=" + $details.Length)

# Required-opinions rejection
$token = Get-AntiforgeryToken $partner '/PartnerDashboard'
$body = 'assignmentId=' + $assignmentId +
        '&returnOnly=true' +
        '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($token)
$null = Post-Page $partner '/PartnerDashboard/Submit' $body
$detailsAfter = Get-Page $partner ("/PartnerDashboard/Details/" + $assignmentId)
Record "Submit(returnOnly=true) empty comment - required-opinions message shown" "yes" $(if ($detailsAfter.Contains('Required opinions error message')) {'yes'} else {'no'})

Write-Host ""
Write-Host "STEP 4: PARTNER SUBMIT WITH SCORES + OPINION"
Write-Host "--------------------------------------------------------------------"
$token = Get-AntiforgeryToken $partner '/PartnerDashboard'
$criterionIds = @($detailsAfter | Select-String -Pattern 'name="score_([0-9a-f-]{36})"' -AllMatches | ForEach-Object { $_.Matches[0].Groups[1].Value }) | Select-Object -Unique
Write-Host ("Criteria ids: " + ($criterionIds -join ', '))

$body = 'assignmentId=' + $assignmentId + '&returnOnly=false&comment=Test+opinion'
$idx = 1
foreach ($c in $criterionIds) {
    $body += ('&score_' + $c + '=' + $idx + '&comment_' + $c + '=comment-' + $idx)
    $idx++
}
$body += '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($token)
$r = Post-Page $partner '/PartnerDashboard/Submit' $body
Record "Submit scores+opinion HTTP 302 redirect" "302" ([string]$r.Status)

$detailsFinal = Get-Page $partner ("/PartnerDashboard/Details/" + $assignmentId)
Record "Submit success TempData shows up"     "shown" $(if ($detailsFinal.Contains('Submit success TempData')) {'shown'} else {'no'})
Record "Total score line appears after submit" "yes" $(if ($detailsFinal.Contains('Total score line')) {'yes'} else {'no'})

Write-Host ""
Write-Host "STEP 5: NOT-COMPETENT 3-DAY WINDOW + REASON"
Write-Host "--------------------------------------------------------------------"

$dashboard3 = Get-Page $partner '/PartnerDashboard'
$match2 = ($dashboard3 | Select-String -Pattern 'href="/PartnerDashboard/Details/([0-9a-f-]{36})"' -AllMatches | Select-Object -First 1)
if (-not $match2) {
    Write-Host "No second assignment available - skipping not-competent test"
} else {
    $assignmentId2 = $match2.Matches[0].Groups[1].Value
    Write-Host ("Second assignment: " + $assignmentId2)
    $token = Get-AntiforgeryToken $partner '/PartnerDashboard'
    $body = 'assignmentId=' + $assignmentId2 +
            '&reason=Wrong+department+routing' +
            '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($token)
    $r = Post-Page $partner '/PartnerDashboard/ReturnNotCompetent' $body
    Record "ReturnNotCompetent HTTP 302 redirect" "302" ([string]$r.Status)
    $details2After = Get-Page $partner ("/PartnerDashboard/Details/" + $assignmentId2)
    Record "Red mis-route badge renders after not-competent" "shown" $(if ($details2After.Contains('Red badge mis-route')) {'shown'} else {'no'})
    Record "Reason surfaces after not-competent"           "shown" $(if ($details2After.Contains('Wrong department routing')) {'shown'} else {'no'})
    Record "Success alert for not-competent shown"        "shown" $(if ($details2After.Contains('Not-competent success alert')) {'shown'} else {'no'})

    # Empty reason rejection
    $token = Get-AntiforgeryToken $partner '/PartnerDashboard'
    $body = 'assignmentId=' + $assignmentId2 +
            '&reason=' +
            '&__RequestVerificationToken=' + [System.Web.HttpUtility]::UrlEncode($token)
    $null = Post-Page $partner '/PartnerDashboard/ReturnNotCompetent' $body
    $details2After2 = Get-Page $partner ("/PartnerDashboard/Details/" + $assignmentId2)
    Record "Empty reason for not-competent rejected with Arabic message" "yes" $(if ($details2After2.Contains('Empty reason error message')) {'yes'} else {'no'})
}

Write-Host ""
Write-Host "RESULTS"
$passed = ($results | Where-Object Pass).Count
$failed = ($results | Where-Object { -not $_.Pass }).Count
Write-Host ("Passed: {0}/{1}" -f $passed, $results.Count)
if ($failed -gt 0) {
    Write-Host "Failed:" -ForegroundColor Red
    $results | Where-Object { -not $_.Pass } | ForEach-Object {
        Write-Host (" - {0} (expected {1}, got {2}) {3}" -f $_.Name, $_.Expected, $_.Actual, $_.Detail) -ForegroundColor Red
    }
    exit 1
}
Write-Host "All checks passed." -ForegroundColor Green