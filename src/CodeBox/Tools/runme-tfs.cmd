:: ----------------------------------------
:: Turn echo off unless verbose is defined.
:: ----------------------------------------
@echo off
if defined Verbose echo on

@echo Preparing the CodeBox environment
@echo.
@echo Using TFS for source control.
@echo CodeBox project is %CODEBOXPROJECT%
@echo.

@echo Adding %inetroot%\CodeBox\tools to path.
set path=%path%;%inetroot%\CodeBox\tools

@echo Updating local tools from server
call CopyFilesFromShare.cmd

if exist "%inetroot%\CodeBox\override\Tools\runme.cmd" (
  @echo Calling "%inetroot%\CodeBox\override\Tools\runme.cmd"
  "%inetroot%\CodeBox\override\Tools\runme.cmd"
  goto :End
)

set _VSTOOLS=%VS120COMNTOOLS%
if "%_VSTOOLS%"=="" set _VSTOOLS=%VS110COMNTOOLS%
if "%_VSTOOLS%"=="" set _VSTOOLS=%VS100COMNTOOLS%
if "%_VSTOOLS%"=="" set _VSTOOLS=%VS90COMNTOOLS%
if "%_VSTOOLS%"=="" set _VSTOOLS=%VS80COMNTOOLS%

if "%_VSTOOLS%"=="" (
  @echo Warning: Could not find variable VS120COMNTOOLS, VS110COMNTOOLS, VS100COMNTOOLS, VS90COMNTOOLS or VS80COMNTOOLS.
  @echo          Visual Studio 2005, 2008, 2010, 2012 or 2013 may not be installed.  Not running vsvars32.bat.
) else (
  if not exist "%_VSTOOLS%vsvars32.bat" (
    @echo Warning: Could not find "%_VSTOOLS%vsvars32.bat"
  ) else (
    call "%_VSTOOLS%vsvars32.bat"
  )
)

:End
@echo.

