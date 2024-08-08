import jwtDecode from "jwt-decode";
import { UserInfo } from "os";
import IJwtAuthResponse from "src/models/Auth/IJwtAuthResponse";
import IJwtInfo from '../models/Common/IJwtInfo';
import IUserInfoFromJwt from '../models/Auth/IUserInfoFromJwt';

export default class JwtHelper {
    // !reviewed 3 apr 2023
    public static DecodeAccessToken = (accessJwt: string) => {
        console.info("Deconding access token using jwtHelper...");

        var decoded = jwtDecode<IUserInfoFromJwt>(accessJwt);
        console.info("Decoded into object:");
        console.info(decoded);

        if (!this.ValidateLifetime(decoded)) {
            console.error("Lifetime validation failed.");
            return undefined;
        }

        console.info("Returning from jwtHelper...");
        return decoded;
    }

    // !reviewed 3 apr 2023
    public static ValidateLifetime = (decodedJwt: IJwtInfo) => {
        let exp = new Date(decodedJwt.exp * 1000);
        // sometimes server responds so fast that nbf is binded for the next second
        // and fails this validation, so we give it a 1 second space
        let nbf = new Date((decodedJwt.nbf-1) * 1000);
        let now = new Date(Date.now());
        console.info(`Validating token lifetime:\nexp= ${exp},\nnbf= ${nbf},\nnow= ${now}.`);

        if (exp < now) return false;
        if (nbf > now) return false;

        return true;
    }
}
