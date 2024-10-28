.Net/C# port of awesome antirez kilo text editor https://github.com/antirez/kilo. Based on another awesome implementation described in [Build Your Own Text Editor](https://viewsourcecode.org/snaptoken/kilo/).
Expected to work in Unix and Windows terminals (cmd, powershell, wsl).

## Screens

![image](https://github.com/user-attachments/assets/4cc6457d-deca-49e0-ae19-64dccc5663cc)

## Prerequisites

* .NET 8.0 or later
``` shell
dotnet --version
# Expected output: 8.0.X
```

<details>
  <summary>HOWTO: Install .NET 8 on Windows, Linux, and macOS</summary>
  
### Windows
``` shell
# run in elevated shell
winget install Microsoft.DotNet.SDK.8
```

### Ubuntu
``` shell
# Register Microsoft packages feed (https://learn.microsoft.com/en-us/linux/packages)
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# installation
sudo apt update && sudo apt-get install -y dotnet-sdk-8.0
```

### MacOS
``` shell
brew install dotnet
```

### See also
[Install .NET on Windows, Linux, and macOS](https://learn.microsoft.com/en-us/dotnet/core/install/)

</details>

## Usage

``` bash
git clone https://github.com/AndreyRusyaev/dotnet-kilo/
cd dotnet-kilo
dotnet run
```

## Changes

* abstracted VT100 sequences (see VT100.cs)
* Unicode support
* Support for Windows terminals (CMD, Powershell, WSL).
* Alternate screen mode support (XTERM 1049 mode)

## Other remarkable kilo ports/implementations

* [kiro-editor (Rust)](https://github.com/rhysd/kiro-editor)
* [kibi (Rust)](https://github.com/ilai-deutel/kibi)
* [hecto (Rust)](https://github.com/pflenker/hecto-tutorial) via [hecto: Build Your Own Text Editor in Rust](https://www.flenker.blog/hecto/)
* [gram (Zig)](https://github.com/eightfilms/gram)
* [kilo.zig (Zig)](https://github.com/h4rr9/kilo.zig)
* [kilo-in-go (Go)](https://github.com/bediger4000/kilo-in-go)
