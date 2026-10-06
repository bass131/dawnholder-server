# Definitions only. SQL callers share one numeric boundary without retaining provider text or inner errors.
function New-DatabaseSqlFailure(
    [Exception]$Exception
) {
    $number = 0
    $current = $Exception
    while ($null -ne $current) {
        # PowerShell can wrap provider errors; trust only the first SqlException's CLR Number.
        if ($current -is [Data.SqlClient.SqlException]) {
            $providerNumber = $current.PSBase.Number
            if ($providerNumber -is [int]) {
                $number = $providerNumber
            }
            break
        }
        $current = $current.PSBase.InnerException
    }
    $safeError = [InvalidOperationException]::new(
        "Test environment SQL command failed (provider number $number); " +
        'raw SQL and provider text suppressed. Preserve manifest.'
    )
    $safeError.Data['DatabaseSqlNumber'] = $number
    return $safeError
}
