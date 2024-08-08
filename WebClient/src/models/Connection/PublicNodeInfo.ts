export interface IPublicNodeInfo {
    id: number;
    name: string;
    ipAddress: string;
    wireguardPort: number;
    userAccessLevelRequired: number;
    isActive: boolean;
    clientsConnected: number;
}