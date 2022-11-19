#### [Back](../auth.md) to reference

---

Validates token and returns new one if it is valid


### Name: `auth.validateToken`
### URL: `https://ivanyudin.alwaysdata.net/api/auth.validateToken`
### Method: `POST`
---

## Body
```jsonc
{
  "token": "string"
}
```
---
## Response
```jsonc
{
  "token": "string",
  "accessType": "number" // enum AccessType: user = 0, admin = 1, developer = 2
}
```