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
         content: username=<combo.username>&password=<combo.password>&token=<token>
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
