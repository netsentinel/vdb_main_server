[![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white)](https://hub.docker.com/repository/docker/luminodiode/rest2wireguard)
[![Alpine Linux](https://img.shields.io/badge/Alpine_Linux-%230D597F.svg?style=for-the-badge&logo=alpine-linux&logoColor=white)](https://www.alpinelinux.org)
[![Postgres](https://img.shields.io/badge/postgres-%23316192.svg?style=for-the-badge&logo=postgresql&logoColor=white)](https://www.npgsql.org/)
[![Nginx](https://img.shields.io/badge/nginx-%23009639.svg?style=for-the-badge&logo=nginx&logoColor=white)](https://nginx.org)
[![.Net](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/en-us/apps/aspnet)
# vdb_main_server
### Alpine-based TLS-securely WebAPI-managed controller server for [rest2wg](https://github.com/LuminoDiode/rest2wireguard) containers network.
<br/>

## Full list of endpoints:
- ### AUTH
    - **GET /api/auth** - always returns 200_OK if user is authorized.
    - **POST /api/auth[?provideRefresh=true][?refreshJwtInBody=false]** - authenticates the user using [LoginRequest](https://github.com/LuminoDiode/vdb_main_server/blob/master/vdb_main_server_api/Models/UserApi/Website/Auth/LoginRequest.cs) credentials.
    - **PUT /api/auth[?provideRefresh=true][?refreshJwtInBody=false][?redirectToLogin=false]** - creates [or authenticates (if redirectToLogin is set to true)] the user using [RegistrationRequest](https://github.com/LuminoDiode/vdb_main_server/blob/master/vdb_main_server_api/Models/UserApi/Website/Auth/RegistrationRequest.cs) credentials.
    - **PATCH /api/auth** - refreshes tokens using refresh JWT from cookie XOR [RefreshJwtRequest](https://github.com/LuminoDiode/vdb_main_server/blob/master/vdb_main_server_api/Models/UserApi/Website/Auth/RefreshJwtRequest.cs) from body. If token is passed both in cookie and body, 400_BadRequest is returned.
    - **PATCH /api/auth/refresh** - do the same as above.
    - **DELETE /api/auth** - terminates all other refresh JWTs. Token must be passed in cookies.
- ### DEVICE (requires authorizaion)
    - **GET /api/device** - returns list of devices for current user.
    - **PUT /api/device** - adds new device to the database using [AddDeviceRequest](https://github.com/LuminoDiode/vdb_main_server/blob/master/vdb_main_server_api/Models/UserApi/Website/Device/AddDeviceRequest.cs) body.
    


## Full list of environment variables
- ### NGINX
    - **VDB_LIMIT_REQ** - limit requests per second for every address (0.1 = 6 requests per minute etc.).
        - Valid range: 0.001<VALUE<10^7. 
        - Default: 100000.
    - **VDB_ALLOWED_IP** - allow requests only from specified address. 
        - Valid range: any IP-address. 
        - Default: *all*.
- ### ASP WebAPI
    - **VDB_GENERATE_JWT_SIG** - generate random JWT signing key on container first run.
        - Valid range: true/false.
        - Default: true.

## Full list of listened ports
- **5001** - nginx-to-api HTTP2 self-signed TLS port.
- **5002** - nginx-to-api no-TLS port.
