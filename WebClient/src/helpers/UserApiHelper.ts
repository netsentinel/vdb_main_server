import axios from "axios";
import UrlHelper from "./UrlHelper";
import IUserDevice from '../models/Device/IUserDevice';
import GlobalContext from './GlobalContext';
import DeleteDeviceRequest from '../models/Device/DeleteDeviceRequest';
import AuthHelper from './AuthHelper';
import ISessionsResponse from "src/models/Auth/ISessionsResponse";
import IJwtAuthResponse from '../models/Auth/IJwtAuthResponse';
import EnvHelper from './EnvHelper';
import { Console } from "console";
import { ChangePasswordRequest } from '../models/Auth/ChangePasswordRequest';

/* This class must be transient sience it has no ability
 * to refresh access token and will simply fail if it 
 * expire during the instance lifetime.
 */
export default class UserApiHelper {
    private _lastStatus?: number;
    public get lastStatus(): number | undefined {
        return this._lastStatus;
    }

    private _accessJwt: string;
    constructor() {
        this._accessJwt = "";
    }

    // !reviewed 3 apr 2023
    private onRequest = async () => {
        if ((await AuthHelper.EnsureUserInContext()) && GlobalContext.lastLoadedAccessJwt) {
            this._accessJwt = GlobalContext.lastLoadedAccessJwt;
        } else {
            throw Error("No JWT was loaded.");
        }
    }

    // !reviewed 3 apr 2023
    public getUserDevices = async () => {
        await this.onRequest();
        try {
            var response = await axios.get<IUserDevice[]>(UrlHelper.getDeviceUrl(), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to load devices.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status !== 200) {
            console.error(`Failed to load devices: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        console.info("Loaded devices. Ids: "
            + `${response.data.length > 0 ? response.data.map(x => x.id).join(' ') : "none"}. `
            + `Count: ${response.data.length}.`);
        this._lastStatus = response.status;
        return response.data;
    }

    // !reviewed 3 apr 2023
    public removeUserDevice = async (devicePubkey: string) => {
        await this.onRequest();
        try {
            var response = await axios.patch<IUserDevice[]>(UrlHelper.getDeviceUrl(),
                new DeleteDeviceRequest(devicePubkey), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to remove device.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return false;
        }

        if (!(response.status >= 200 && response.status <= 299)) {
            console.error(`Failed to load devices: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return false;
        }

        return true;
    }

    public getUserSessions = async () => {
        await this.onRequest();
        try {
            var response = await axios.get<ISessionsResponse>(UrlHelper.getSessionsUrl(), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to load sessions.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status !== 200) {
            console.error(`Failed to load sessions: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        console.info(`Loaded other sessions. Count: ${response.data.totalCount}`);
        this._lastStatus = response.status;
        return response.data;
    }

    public terminateOtherSessions = async () => {
        await this.onRequest();
        try {
            var response = await axios.delete<IJwtAuthResponse>(UrlHelper.getAuthUrl(), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to terminate sessions.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return false;
        }

        if (!(response.status >= 200 && response.status <= 299)) {
            console.error(`Failed to terminate sessions: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return false;
        }

        console.info(`Terminated other sessions.`);
        this._lastStatus = response.status;
        AuthHelper.HandleLoginOrRegisterRenpose(response);
        return true;
    }
    public terminateThisSession = async () => {
        await this.onRequest();
        try {
            var response = await axios.delete(UrlHelper.getCurrentSessionTerminationUrl(), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to terminate session.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return false;
        }

        if (!(response.status >= 200 && response.status <= 299)) {
            console.error(`Failed to terminate session: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return false;
        }

        console.info(`Terminated this session.`);
        this._lastStatus = response.status;
        return true;
    }
    public changePassword = async (newPass: string) => {
        await this.onRequest();
        try {
            var response = await axios.patch(UrlHelper.getPasswordPatchUrl(),
                new ChangePasswordRequest(newPass), {
                headers: {
                    'Authorization': ('Bearer ' + this._accessJwt)
                }
            });
        } catch (e: any) {
            console.error("Failed to change password.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return false;
        }

        if (!(response.status >= 200 && response.status <= 299)) {
            console.error(`Failed to change password: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return false;
        }

        console.info(`Password was changed.`);
        this._lastStatus = response.status;
        return true;
    }
}