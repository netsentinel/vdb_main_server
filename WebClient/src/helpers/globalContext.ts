import jwtDecode from "jwt-decode";
import { UserInfo } from "os";
import IJwtAuthResponse from "src/models/Auth/IJwtAuthResponse";
import IUserInfo from "src/models/Auth/IUserInfo";
import IJwtInfo from '../models/Common/IJwtInfo';
import IUserInfoFromJwt from '../models/Auth/IUserInfoFromJwt';

export default class GlobalContext {
    public static currentUser?: IUserInfoFromJwt;
    public static lastLoadedAccessJwt: string | null;
    public static refreshJwtExpires: number | null;

    // !reviewed 3 apr 2023
    public static get AccessLifespanMs(): number | undefined {
        return this.currentUser
            ? (this.currentUser.exp - this.currentUser.nbf) * 1000
            : undefined;
    }

    private static readonly accessTokenName: string = "accessTokenJwt";

    public static setAccessJwtInStorage = (token: string) => {
        localStorage.setItem(this.accessTokenName, token);
    }
    public static getAccessJwtFromStorage = () => {
        let result = localStorage.getItem(this.accessTokenName);
        this.lastLoadedAccessJwt = result;
        return result;
    }


    public static GetAccessLevel = () => {
        if(this.currentUser === undefined) return undefined;
        let user = this.currentUser;

        console.info("Calculating user access level...");

        if (user.IsAdmin === true) { 
            console.log("User is admin. Level 3.");
            return 3;
        } else{
            console.log("User is not admin. Decreasing...");
        }
        if (Date.parse(user.PayedUntilUtc) > Date.now()){ 
            console.log("User has subscription. Level 2.");
            return 2;
        } else{
            console.log("User  hasn't subscription. Decreasing...");
        }
        if (user.IsEmailConfirmed === true){
            console.log("User email is confirmed. Level 1.");
            return 1;
        } else{
            console.log("User email isn't confirmed. Decreasing...");
        }

        console.log("None of the conditions met. Level 0.")
        return 0;
    };

    private static readonly accessTokenExpDateName: string = "refreshExpDate";

    public static setRefreshExpInStorage = (refreshExpDate: number) => {
        localStorage.setItem(this.accessTokenExpDateName, refreshExpDate.toString());
    }
    public static getRefreshExpFromStorage = () => {
        let value = localStorage.getItem(this.accessTokenExpDateName);
        return value ? parseInt(value) : undefined;
    }   

    public static logout = () => {
        console.info("Logging out: removing user info...")

        localStorage.removeItem(this.accessTokenName);
        localStorage.removeItem(this.accessTokenExpDateName);

        this.currentUser = undefined;
        this.lastLoadedAccessJwt = this.refreshJwtExpires = null;
    }

    public static latestVersionHref: string | undefined;
}
