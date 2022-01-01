# googlr
Cmd-line Google search for Windows! (Inspired from [Googler for Linux](https://github.com/jarun/googler))

---

> **USAGE**: 

**`googlr`**

  ![Screenshot](https://raw.githubusercontent.com/vamsitp/googlr/master/Screenshot.png)
  > [Common search techniques](https://support.google.com/websearch/answer/2466433)

- Enter the `search-phrase` for general Search (e.g. **`"cosmos db" site:stackoverflow.com`**)
- Enter `/` followed by the `search-phrase` for News-search (e.g. **`/microsoft azure`**)
- Enter the `index` to open the corresponding link (in default browser)
- Enter `c` to clear the console
- Enter `q` to quit
- Enter `+` to update to the latest version
- Enter `?` to print this help

---

> **PRE-REQ**: [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet-core/6.0)
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

> ##### CONTRIBUTION
```batch
# Install from local project path
dotnet tool install -g --add-source ./bin googlr

# Publish package to nuget.org
nuget push ./bin/Googlr.1.0.0.nupkg -ApiKey <key> -Source https://api.nuget.org/v3/index.json
```
