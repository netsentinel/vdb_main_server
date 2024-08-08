import UserApiHelper from "./UserApiHelper";
import LoginRequest from '../models/Auth/LoginRequest';
import axios from 'axios';
import IJwtAuthResponse from '../models/Auth/IJwtAuthResponse';
import UrlHelper from './UrlHelper';
import GlobalContext from "./GlobalContext";
import JwtHelper from './JwtHelper';
import { AxiosResponse } from "axios";
import RegistrationRequest from '../models/Auth/RegistrationRequest';

/* This static class provides actions which affects
 * globalContext fields.
 */
export default class AuthHelper {
    private static _lastStatus?: number;
    public static get lastStatus(): number | undefined {
        return this._lastStatus;
    }

    // !reviewed 3 apr 2023
    private static ParseAccessJwtAndUpdateContext = (accessToken: string): boolean => {
        console.info(`Parsing access token \'${accessToken}\'...`)

        var user = JwtHelper.DecodeAccessToken(accessToken);
        if (!user) {
            console.error("Unable to parse access token.");
            return false;
        }

        console.info("Saving JWT into local storage...")
        GlobalContext.setAccessJwtInStorage(accessToken);

        console.info("Setting last loaded token...");
        GlobalContext.lastLoadedAccessJwt = accessToken;

        console.info("Setting current user...");
        GlobalContext.currentUser = user;

        console.info(`Current user: ${GlobalContext.currentUser.Email}.\n` +
            `Access JWT expires: ${new Date(user.exp * 1000)}.`)

        return true;
    }

    // !reviewed 3 apr 2023
    private static ParseResponseAndUpdateContext = (result: AxiosResponse<IJwtAuthResponse, any>): boolean => {
        console.info("Parsing response from auth endpoint...");
        this._lastStatus = result.status;

        if (result.status === 200) {
            if (result.data.refreshExpires) {
                console.info(`Server provided refresh token with expiration date: ${result.data.refreshExpires}.`);
                GlobalContext.refreshJwtExpires = result.data.refreshExpires;
                GlobalContext.setRefreshExpInStorage(result.data.refreshExpires)
            }
            return this.ParseAccessJwtAndUpdateContext(result.data.accessToken);
        } else {
            console.error("Returned status was not equal to 200.");
            return false;
        }
    }

    private static delay = (delayMs: number) => {
        return new Promise(resolve => setTimeout(resolve, delayMs));
    }
    private static IsNowRefreshing: boolean = false;
    // !reviewed 3 apr 2023
    public static EnsureUserInContext = async (): Promise<boolean> => {
        let delayCount = 0;
        while (this.IsNowRefreshing) {
            if (delayCount++ % 10 === 0) console.warn("EnsureUserInContext is delayed during another call.");
            if (delayCount > 100) break;
            await this.delay(100);
        }
        this.IsNowRefreshing = true;
        setTimeout(() => this.IsNowRefreshing = false, 1000);

        try {
            console.info("Trying to load access token...");
            const curr = GlobalContext.currentUser;

            if (curr) { // loaded user already exists
                const lifespanSecs = (curr.exp - curr.nbf);
                // 10% of JWT lifespan
                const minimumMilliseconds = lifespanSecs / 10 * 1000;
                const currExpMs = curr.exp * 1000;

                console.info(`Access token lifespan is ${Math.round(lifespanSecs)} seconds.`);
                const currTimeSec = Date.now() / 1000;
                const timeLeft = curr.exp - currTimeSec;
                const percentLeft = (timeLeft) / (lifespanSecs) * 100;
                console.info(`Currently time left: ${timeLeft > 0 ? Math.round(timeLeft) : 0} seconds`
                    + ` (${Math.round(percentLeft > 0 ? Math.round(percentLeft) : 0)}%).`);


                if (currExpMs < Date.now()) { // token expired
                    GlobalContext.logout();
                    console.info("Access token already loaded, but expired. Refreshing...");
                    try { await this.PerformRefreshAsync(); }
                    catch { return false; }
                    return true;
                }
                else if (currExpMs < Date.now() + minimumMilliseconds) { // token will expire soon
                    console.info("Access token already loaded, but will expire soon. Refreshing...");
                    try { await this.PerformRefreshAsync(); } catch { }
                    return true;
                }
                else { // enough time remains
                    console.info("Access token already loaded.");
                    return true;
                }
            }

            // no current user, try to load from localstorage
            console.info("Loading token from local storage...");
            const jwt = GlobalContext.getAccessJwtFromStorage();
            if (jwt && AuthHelper.ParseAccessJwtAndUpdateContext(jwt)) {
                console.info("Calling EnsureUserInContext recursively...");
                return await this.EnsureUserInContext();
            } else if (jwt) {
                console.info("Access token found, but validation failed. Refreshing...");
                let result: boolean = false;
                try {
                    if (await AuthHelper.PerformRefreshAsync()) {
                        result = true;
                    }
                } catch { }
                return result;
            }

            console.info("No access token found.");
            return false;
        } catch {
            return false;
        } finally {
            this.IsNowRefreshing = false;
        }
    }

    // !reviewed 3 apr 2023
    public static HandleLoginOrRegisterRenpose = (response?: AxiosResponse<IJwtAuthResponse, any>): boolean => {
        if (response) {
            this._lastStatus = response.status;
            try {
                return this.ParseResponseAndUpdateContext(response);
            }
            catch (e) {
                console.error(e);
            }
        }
        return false;
    }

    // !reviewed 3 apr 2023
    public static PerformLoginAsync = async (request: LoginRequest): Promise<boolean> => {
        console.info("Trying to log in...");
        this._lastStatus = 0;

        try {
            var response = await axios.post<IJwtAuthResponse>(UrlHelper.getAuthUrl(), request);
        } catch (e: any) {
            console.error("Failed to login.");
            let code = e["response"]["status"];
            if (code) this._lastStatus = code;
            return false;
        }

        return this.HandleLoginOrRegisterRenpose(response);
    }

    // !reviewed 3 apr 2023
    public static PerformRegistrationAsync = async (request: RegistrationRequest, redirectToLogin = true) => {
        console.info("Trying to register user...");
        console.info(`Redirect to login in case of existing user is ${redirectToLogin ? "enabled" : "disabled"}.`);
        this._lastStatus = 0;

        try {
            var response = await axios.put<IJwtAuthResponse>(UrlHelper.getAuthUrl() + `?redirectToLogin=${redirectToLogin}`, request);
        } catch (e: any) {
            console.error("Failed to register.");
            let code = e["response"]["status"];
            if (code) this._lastStatus = code;
            return false;
        }

        return this.HandleLoginOrRegisterRenpose(response);
    }

    // !reviewed 3 apr 2023
    public static PerformRefreshAsync = async () => {
        console.info("Trying to refresh tokens...");
        this._lastStatus = 0;

        try {
            var response = await axios.patch<IJwtAuthResponse>(UrlHelper.getRefreshUrl());
        } catch (e: any) {
            console.error("Failed to refresh tokens.");
            let code = e["response"]["status"];
            if (code) this._lastStatus = code;
            return false;
        }

        return this.HandleLoginOrRegisterRenpose(response);
    }
}