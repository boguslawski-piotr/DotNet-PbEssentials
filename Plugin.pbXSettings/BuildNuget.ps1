./IncBuild.ps1

MSBuild ./Plugin.pbXSettings.sln /t:Clean,Rebuild /p:Configuration="Release" /p:Platform="Any CPU" /verbosity:m /nowarn:CS4014,CS1591,CS1998,CS1574,VSX1000

nuget pack ./Plugin.pbXSettings.nuspec
