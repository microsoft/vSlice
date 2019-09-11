' testPowerShell.vbs
'
' Windows Scripting Host VBScript program
' Tests to see if PowerShell is intalled.
Set oShell = CreateObject("WScript.Shell")

Function IsPowerShellInstalled()
    On Error Resume Next
    Set oExec = oShell.Exec("powershell -WindowStyle hidden >nul 2>nul")
    If (oExec Is Nothing) Then
        On Error Goto 0
        exitCode = 1
    Else
    On Error Goto 0
        exitCode = oExec.ExitCode
        Set oExec = Nothing
    End If

    If exitCode = 0 Then
        result = true
    Else
        result = false
    End If
    IsPowerShellInstalled = result
End Function

If IsPowerShellInstalled() = false Then
    WScript.Quit 1
End If
