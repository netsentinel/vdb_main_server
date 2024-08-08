import jwtDecode from "jwt-decode";
import { UserInfo } from "os";
import IJwtAuthResponse from "src/models/Auth/IJwtAuthResponse";
import IJwtInfo from '../models/Common/IJwtInfo';
import IUserInfoFromJwt from '../models/Auth/IUserInfoFromJwt';
import authSettings from "../config/authSettings.json"

export default class ValidationHelper {

    public static ValidatePasswordAndGetError = (password: string) => {
        if (
            password.length < authSettings.passwordMinLength ||
            password.length > authSettings.passwordMaxLength ||
            (authSettings.passwordMustContainDigits && !(/\d/.test(password))) || // has digit
            (authSettings.passwordMustContainUpperCase && password.toLowerCase() === password) ||
            (authSettings.passwordMustContainLowerCase && password.toUpperCase() === password)
        ) {
            console.info("Password failed client-side validation.");
            let error =
                `Password must be ${authSettings.passwordMinLength} ` +
                `to ${authSettings.passwordMaxLength} characters long.`;

            if (!(authSettings.passwordMustContainDigits ||
                authSettings.passwordMustContainUpperCase ||
                authSettings.passwordMustContainLowerCase)) {
                return error;
            }

            error += ` It must contain at least`;

            if (authSettings.passwordMustContainDigits) {
                error += ` one digit`;
            }
            if (authSettings.passwordMustContainLowerCase) {
                if (!error.endsWith('least'))
                    if (authSettings.passwordMustContainUpperCase)
                        error += ',';
                    else
                        error += ' and'

                error += ` one lower case letter`;
            }
            if (authSettings.passwordMustContainUpperCase) {
                if (!error.endsWith('least')) error += ' and';

                error += ` one upper case letter`;
            }

            error += '.';

            return error;
        } else {
            return undefined;
        }
    }
    public static ValidateEmailAndGetError = (email: string) => {
        if (email
            .toLowerCase()
            .match(
                /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|.(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/
            )) {
            return undefined;
        }
        else {
            return "Email must be in the correct format.";
        }
    };
}