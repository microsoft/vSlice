:: ----------------------------------------
:: Turn echo off unless verbose is defined.
:: ----------------------------------------
@echo off
if defined Verbose echo on

set DevBldcommon= %_NTDEVELOPER%\build\bldcommon.cmd

:: -----------------------------------------------------------
:: Create a default dev environment for this new contributor
::
:: Note:  This is intended for use *only* for initial setup
::        of CodeBox contribution VMs.  
::
:: ---------------------------------------------------------
if not exist "%DevBldcommon%" (
  @echo.
  @echo "Creating %DevBldCommon%"

  @echo set SqlServer=.>> %DevBldCommon%
  @echo set AppUrl=http://localhost:12676>> %DevBldCommon%
  @echo set WebServiceUrl=http://localhost:12676>> %DevBldCommon%
  @echo set AdminSMTPLocalDeliveryDirectory=c:\CodeBox\MailSent>> %DevBldCommon%
  @echo set WebSMTPLocalDeliveryDirectory=c:\CodeBox\MailSent>> %DevBldCommon%
  @echo set CodeBoxHost=localhost>> %DevBldCommon%
  @echo set CodeBoxPort=12676>> %DevBldCommon%
  @echo set ToolBoxHost=localhost1>> %DevBldCommon%
  @echo set ToolBoxPort=12676>> %DevBldCommon%
  @echo set TempDir=c:\CodeBox\Temp>> %DevBldCommon%
  @echo set WorkItemCacheDir=c:\CodeBox\WorkItemCache>> %DevBldCommon%
  @echo set ProjectCreationLoggingPath=c:\CodeBox\logging>> %DevBldCommon%
  @echo set ProjectCreationQueue= %ComputerName%\Private$\TFPQueue>> %DevBldCommon%
  @echo set TFS2008Tools=%ProgramFiles%\Microsoft Visual Studio 9.0\Common7\IDE>> %DevBldCommon%
  @echo set TFS2008PowerTools=%ProgramFiles%\Microsoft Team Foundation Server 2008 Power Tools>> %DevBldCommon%
  @echo set VANGUARDINSTALLDIR=%ProgramFiles%\Microsoft Visual Studio 9.0\Team Tools\Performance Tools>> %DevBldCommon%
)  

@echo.
@echo "Registering SDAPI.DLL."
regsvr32 /s %SolutionDir%\DevLibraries\sdapi.dll

:End
@echo.

