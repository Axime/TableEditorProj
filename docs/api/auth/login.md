#### [Back](../auth.md) to reference

---

Register new user


### Name: `auth.login`
### URL: `https://ivanyudin.alwaysdata.net/api/auth.login`
### Method: `POST`
---

## Body
```jsonc
{
  "username": "string",
  "password": "string",
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