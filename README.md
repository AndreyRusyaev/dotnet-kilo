.Net/C# port of awesome antirez kilo text editor https://github.com/antirez/kilo. Based on another awesome implementation described in [Build Your Own Text Editor](https://viewsourcecode.org/snaptoken/kilo/).
Expected to work in Unix and Windows terminals (cmd, powershell, wsl).

![image](https://github.com/user-attachments/assets/4cc6457d-deca-49e0-ae19-64dccc5663cc)

## Usage

``` sh
git clone https://github.com/AndreyRusyaev/dotnet-kilo/
cd dotnet-kilo
dotnet run
```

## Changes

* abstracted VT100 sequences (see VT100.cs)
* Unicode support
* Support for Windows terminals (CMD, Powershell, WSL).
