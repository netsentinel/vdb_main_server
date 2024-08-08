export default interface IUserDevice {
    id: number;
    userId: number;
    wireguardPublicKey: string;
    lastConnectedNodeId: number | null;
    lastSeenUtc: string | null;
}