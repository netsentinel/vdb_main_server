import AddDeviceRequest from './DeleteDeviceRequest';

export default class DeleteDeviceRequest implements AddDeviceRequest {
    wireguardPublicKey: string;

    constructor(pk: string) {
        this.wireguardPublicKey = pk;
    }
}