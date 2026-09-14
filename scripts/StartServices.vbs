Set WshShell = CreateObject("WScript.Shell")
WshShell.Run "powershell -WindowStyle Hidden -Command ""& 'C:\Program Files\dotnet\dotnet.exe' run --project 'D:\VillageShop_Backend\src\VillageShop.Api\VillageShop.Api.csproj' --urls 'http://0.0.0.0:5000;http://0.0.0.0:8080'""", 0, False
WshShell.Run "powershell -WindowStyle Hidden -Command ""python -m http.server 8085 --directory 'D:\VillageShop_Mobile\build\web'""", 0, False
WshShell.Run "powershell -WindowStyle Hidden -Command ""& 'C:\Users\DELL\.gemini\antigravity\scratch\cloudflared.exe' tunnel --url http://localhost:8085""", 0, False
WshShell.Run "powershell -WindowStyle Hidden -Command ""& 'C:\Users\DELL\.gemini\antigravity\scratch\cloudflared.exe' tunnel --url http://localhost:5000""", 0, False
