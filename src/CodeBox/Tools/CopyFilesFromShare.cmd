xcopy \\tkfiltoolbox\CBXEnlistmentTools\*.* "%INETROOT%\CodeBox\tools\" /E /C /R /H /K /Y /Q /D 
if errorlevel 1 xcopy \\TK5VMBGITCBADM1\EnlistmentTools\*.* "%INETROOT%\CodeBox\tools\" /E /C /R /H /K /Y /Q /D 
