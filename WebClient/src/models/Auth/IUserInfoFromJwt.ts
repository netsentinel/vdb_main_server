import IJwtInfo from '../Common/IJwtInfo';
import IUserInfo from 'src/models/Auth/IUserInfo';


// provides both models as well
export default interface IUserInfoFromJwt extends IJwtInfo, IUserInfo {

}