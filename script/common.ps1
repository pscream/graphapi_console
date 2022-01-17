function Get-UserPassword {

    param (
        [Parameter(Mandatory)]
        [int] $length
    )

    #$charSet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789{]+-[*=@:)}$^%;(_!&amp;#?>/|.'.ToCharArray()
    $charSet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'.ToCharArray()
    $rng = New-Object System.Security.Cryptography.RNGCryptoServiceProvider
    $bytes = New-Object byte[]($length)
 
    $rng.GetBytes($bytes)
 
    $result = New-Object char[]($length)
 
    for ($i = 0 ; $i -lt $length ; $i++) {
        $result[$i] = $charSet[$bytes[$i]%$charSet.Length]
    }
 
    return (-join $result)
}

function Connect-Ad() {
    $adminUsername = Get-Secret -Name AdminUPN -AsPlainText
    $adminPassword = Get-Secret -Name AdminPWD -AsPlainText
    $passwordSecure = ConvertTo-SecureString $adminPassword -AsPlainText -Force
    $psCred = New-Object System.Management.Automation.PSCredential -ArgumentList ($adminUsername, $passwordSecure)
    Connect-AzureAD -Credential $psCred | Out-Null
}

function Disconnect-Ad() {
    Disconnect-AzureAD | Out-Null
}

function Connect-Exchange {
    param (
        [Management.Automation.PsCredential] $creds
    )

    $psCred = $creds

    if ($null -eq $creds)
    {
        $username = Get-Secret -Name AdminUPN -AsPlainText
        $password = Get-Secret -Name AdminPWD -AsPlainText
        $passwordSecure = ConvertTo-SecureString $password -AsPlainText -Force
        $psCred = New-Object System.Management.Automation.PSCredential -ArgumentList ($username, $passwordSecure)
    }
    
    Connect-ExchangeOnline -Credential $psCred | Out-Null
}

function Disconnect-Exchange() {
    Disconnect-ExchangeOnline -Confirm:$false | Out-Null
}