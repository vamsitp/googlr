# googlr
Cmd-line Google search for Windows! (Inspired from [Googler for Linux](https://github.com/jarun/googler))

---

> **PRE-REQ**: [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core/3.0)
```batch
# Install from nuget.org
dotnet tool install -g googlr

# Upgrade to latest version from nuget.org
dotnet tool update -g googlr --no-cache

# Install a specific version from nuget.org
dotnet tool install -g googlr --version 1.0.x

# Uninstall
dotnet tool uninstall -g googlr
```

> **NOTE**: If the Tool is not accesible post installation, add `%USERPROFILE%\.dotnet\tools` to the PATH env-var.

---

> **USAGE**: 

`googlr`

---

> ##### CONTRIBUTION
```batch
# Install from local project path
dotnet tool install -g --add-source ./bin googlr

# Publish package to nuget.org
nuget push ./bin/Googlr.1.0.0.nupkg -ApiKey <key> -Source https://api.nuget.org/v3/index.json
```