@echo off
rem Builds DraculaLauncher.exe with the C# compiler that ships with Windows (.NET Framework 4).
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /codepage:65001 /r:Microsoft.CSharp.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  /out:"%~dp0DraculaLauncher.exe" "%~dp0src\*.cs"
exit /b %errorlevel%
