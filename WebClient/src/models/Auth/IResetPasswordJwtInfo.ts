import IJwtInfo from '../Common/IJwtInfo';
export default interface IResetPasswordJwtInfo extends IJwtInfo {
    emailJwtClaim: string;
}