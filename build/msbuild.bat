
set msbuild="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"

%msbuild% ../src/Raven.RabbitMQClient/Raven.RabbitMQClient.csproj /t:Clean;Rebuild /p:Configuration=Release
xcopy ..\src\Raven.RabbitMQClient\bin\Release ..\output\Raven.RabbitMQClient /i /e /y

pause