import axios from "axios";
import UrlHelper from "./UrlHelper";
import IUserDevice from '../models/Device/IUserDevice';
import GlobalContext from './GlobalContext';
import DeleteDeviceRequest from '../models/Device/DeleteDeviceRequest';
import AuthHelper from './AuthHelper';
import { IPublicNodeInfo } from "src/models/Connection/PublicNodeInfo";
import { IAccessLevelToDevicesLimit } from "src/models/Device/AccessLevelToDevicesLimit";
import { ChangePasswordRequest } from '../models/Auth/ChangePasswordRequest';

export default class ApiHelper {
    private static _lastStatus?: number;
    public static get lastStatus(): number | undefined {
        return this._lastStatus;
    }
    public static GetLastStatus = ()=>this._lastStatus;


    public static getNodesList = async () => {
        try {
            var response = await axios.get<IPublicNodeInfo[]>(UrlHelper.getNodesUrl());
        } catch (e: any) {
            console.error("Failed to load nodes.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status !== 200) {
            console.error(`Failed to load nodes: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        return response.data;
    }
    public static getLatestReleaseUrl = async () => {
        try {
            var response = await axios.get<string>(UrlHelper.getLatestReleaseUrl());
        } catch (e: any) {
            console.error("Failed to load latest release link.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status !== 200) {
            console.error(`Failed to load latest release link: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        //console.log("Received latest release url from server");
        //console.log(response.data);

        return response.data;
    }


    public static getDevicesLimits = async () => {
        try {
            var response = await axios.get<IAccessLevelToDevicesLimit[]>(UrlHelper.getDevicesLimitsUrl());
        } catch (e: any) {
            console.error("Failed to load limits.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status !== 200) {
            console.error(`Failed to load limits: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        return response.data;
    }

    public static resetPassword = async (email: string) => {
        try {
            var response = await axios.put(UrlHelper.getResetPasswordUrl(email));
        } catch (e: any) {
            console.error("Failed to reset password.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status) {
            if (response.status !== 200)
                console.error(`Failed to reset password: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        return undefined;
    }
    public static resetNewPassword = async (jwt: string, request: ChangePasswordRequest) => {
        try {
            var response = await axios.post(UrlHelper.getResetNewPasswordUrl(jwt), request);
        } catch (e: any) {
            console.error("Failed to reset password.");
            if (e["response"]["status"]) {
                this._lastStatus = e["response"]["status"];
            }
            return undefined;
        }

        if (response.status) {
            if (response.status !== 200)
                console.error(`Failed to reset password: HTTP_${response.status}.`);
            this._lastStatus = response.status;
            return undefined;
        }

        return undefined;
    }
}