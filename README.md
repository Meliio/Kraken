# Kraken

Kraken is a tool for brute force or credential stuffing attacks high performance

![screen-gif](./gif.gif)

## Features
* multithreading
* proxy http, sock4 and sock5 support
* saving the progress of the checker
* automatic banning of proxies if not working or bot status ends with ban

## Getting Started

download [release](https://github.com/Meliio/Kraken/releases)
open a cmd then write "cd {path where kraken.exe is located}" then "kraken.exe --help"

### Prerequisite

[.NET Runtime 6.0.0](https://dotnet.microsoft.com/download/dotnet/6.0)


### Usage

| Flag             | Description                                                | Example                                     |
| ---------------- | ---------------------------------------------------------- | --------------------------------------------|
| -c               | yaml file of the config                                    | kraken.exe -c config.yaml                   |
| -w               | wordlist file                                              | kraken.exe -w wordlist.txt                  |
| -p               | proxies file                                               | kraken.exe -p proxies.txt http              |
| -s               | skip                                                       | kraken.exe -s 50                            |
| -b               | number of bots                                             | kraken.exe -b 10                            |
| -v               | verbose                                                    | kraken.exe -v true                          |

### Example

##### Variables

* input
* input.user
* input.pass

* data.statusCode
* data.address
* data.headers
* data.header[headerName]
* data.cookies
* data.cookie[cookieName]
* data.source

```
[SETTINGS]
{
   "name":"example.com",
   "customInputs":[],
   "inputRules":[]
}

[SCRIPT]
REQUEST POST "https://example.com" Redirect=true LoadContent=true
  CONTENT "username=<input.USER>&password=<input.PASS>"
  CONTENTTYPE "application/x-www-form-urlencoded"
  HEADER "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36" 
  HEADER "Pragma: no-cache" 
  HEADER "Accept: */*" 

PARSE "<data.Source>" LR "<title>" "</title>" -> VAR "title" 

KEYCHECK 
  KEYCHAIN Failure OR 
    KEY "bad key" 
  KEYCHAIN Success OR 
    KEY "success key" 
```


### Debug
![debug screen](https://github.com/Meliio/Kraken/blob/main/screen.png)
