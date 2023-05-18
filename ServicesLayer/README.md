# Those services...
## Static
  - **PasswordsService** - simple password salting and hashing.
  - **EnvironmentProvider** - *not currently used*. Easy access to env vars.
## Non-static
  - **SettingsProviderService** - import from prev projects. Easy access to settings and secrets from JSON files.
  - **VpnNodesManipulator** - the primary nodes service. All other services are likely to inject it. Provides most common operations like add/remove peer, ping node.
  - **NodesPublicInfoService** - service for generating public reports about current nodes statuses.
  - **NodesCleanupBackgroundService** - service for calling peers review with the selected interval.
  - **JwtService** - simple JWT create/decode operations.
