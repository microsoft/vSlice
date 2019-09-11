Copy-Item -Path \\tkfiltoolbox\CBXEnlistmentTools\* -Destination $Env:INETROOT\CodeBox\tools\ -Force -Recurse 
if (! $?) { Copy-Item -Path \\TK5VMBGITCBADM1\EnlistmentTools\* -Destination $Env:INETROOT\CodeBox\tools\ -Force -Recurse } 
