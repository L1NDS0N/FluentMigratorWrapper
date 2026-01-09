dotnet restore
dotnet build
dotnet pack
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper


dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/x64 -p:EnableCompressionInSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/x64 -p:EnableCompressionInSingleFile=true
dotnet publish -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=partial -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/x86 -p:EnableCompressionInSingleFile=true