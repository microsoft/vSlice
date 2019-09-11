Steps to get the NSIS install working
	1) Install the nuget package for NSIS 

	2) Add a install.nsi file and have it copy to the output filder
		(Copy the contents form another project and modify)
		Make sure to "save with encodeing" as Western European (Windows) - Codepage 1252

	3) Add build steps (Change the .exe filenames to match your app.  Also change the location of packages if necessary):
		PostBuild: 
			cd $(TargetDir)
			if EXIST vSliceSetup.exe del vSliceSetup.exe
			"$(ProjectDir)\packages\NSIS.2.51\tools\makensis.exe" install.nsi


Steps to publish a new version:

	- Update app version
		- Edit assembly version in Project Properties / Application / Assembly Information
		- Edit install.nsi to have the correct version
		- Edit vSliceSetup.exe.ver to have the current version
	- Build
	- Upload vSliceSetup.exe and vSliceSetup.exe.ver to tools folder on storage account on azure: 
		https://ms.portal.azure.com/#blade/Microsoft_Azure_Storage/FileShareMenuBlade/overview/storageAccountId/%2Fsubscriptions%2F338668da-60c1-455d-b732-c60b90878ad5%2FresourceGroups%2FOSToolsReporting%2Fproviders%2FMicrosoft.Storage%2FstorageAccounts%2Fostoolsinstall/path/tools

Notes:
	Install location: https://ostoolsinstall.blob.core.windows.net/tools/vSliceSetup.exe  () 