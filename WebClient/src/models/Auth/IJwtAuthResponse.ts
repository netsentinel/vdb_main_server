import IJwtInfo from "../Common/IJwtInfo";

export default interface IJwtAuthResponse {
    accessToken: string;
    refreshToken?: string;
    refreshExpires?: number;
}