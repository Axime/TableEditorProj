# API

## Response format

> Format of response is **`JSON`**

Every object contains these properties:

1. If method executing is successful, `"ok"` is `"true"`, `"error"` and `"errorDescription"` are `null`, `"response"` contains result:
  ```jsonc
  {
    "ok": true,
    "error": null,
    "errorDescription": null,
    "response": {/* Method response */}
  }
  ```
2. If execution fails,  `"ok"` will be `false`, *code of error* in `"error"` will be *`non-nullish`* *`non-negative`* value and it's description in `"errorDescription"` will be non-empty string, `"response"` will be `null`:
```jsonc
{
  "ok": false, // executing status
  "error": 0, // Code of error
  "errorDescription": "description of error",
  "response": null
}
```

> We recommend to check `"ok"` before doing something with `"response"` property


## Reference

- [Auth](auth.md)