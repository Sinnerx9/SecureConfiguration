# SecureConfiguration

[![Build Status](https://travis-ci.org/joemccann/dillinger.svg?branch=master)](https://travis-ci.org/joemccann/dillinger)

Introducing a compact solution for safeguarding your appsettings.json file. This package incorporates a hook within the load function, which is triggered during the configuration file's loading process. This feature empowers you to exercise control over the appsettings.json data, granting you permission to modify it before it is configured within the application.

## Installation

SecureConfiguration requires [dotnet core](https://dotnet.microsoft.com/en-us/download) >= 6.0 to run.
```sh
cd myproject
dotnet add package SecureConfiguration
```
## Usage
1 - This line of code imports the `DigitalLogic `namespace in C#.
```csharp
using DigitalLogic;
```
2 - Use secure configuration:
```csharp
var builder = SecureWebApplication.CreateBuilder(args, decryptionFunc);
```
This line of code calls the UseSecureConfiguration method from the SecureConfiguration class, passing the arguments `DigitalLogic` (presumably as the configuration name) and `SecureSettings_OnLoad` (presumably as a callback method to handle the secure settings).

3 - Define the SecureSettings_OnLoad method:
```csharp
string decryptionFunc(string appsettings)
{
    var data = Decrypt(
        Convert.FromBase64String(appsettings),
        Convert.FromBase64String("SGVsbG8sIHRoaXMgaXMgYSAzMi1ieXRlIHN0cmluZy4="),
        Convert.FromBase64String("U2hvcnQgc3RyaW5nLg==")
        );
    Console.WriteLine(data);

    return data;
}
```

4 - Define the Decrypt method:
```csharp
string Decrypt(byte[] body, byte[] key, byte[] iv)
{
    string data = "";
    using Aes aes = Aes.Create();
    aes.Mode = CipherMode.CBC;
    using ICryptoTransform transform = aes.CreateDecryptor(key, iv);
    using MemoryStream ms = new MemoryStream(body);
    using CryptoStream cs = new CryptoStream(ms, transform, CryptoStreamMode.Read);
    using StreamReader sr = new StreamReader(cs);
    data = sr.ReadToEnd();
    return data;
}
```

## License

MIT
