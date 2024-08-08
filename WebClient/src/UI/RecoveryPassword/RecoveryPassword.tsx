import eps from "../../config/endpoints.json"
import jwtDecode from 'jwt-decode';
import IResetPasswordJwtInfo from '../../models/Auth/IResetPasswordJwtInfo';
import { useNavigate } from 'react-router-dom';
import cl from "../VdbSecurity/VdbSecurity.module.css"
import cla from "../AuthForm/AuthForm.module.css"
import { useState, useEffect } from 'react';
import ValidationHelper from '../../helpers/ValidationHelper';
import UserApiHelper from "src/helpers/UserApiHelper";
import JwtHelper from '../../helpers/JwtHelper';
import { CSSTransition } from "react-transition-group";
import ApiHelper from '../../helpers/ApiHelper';
import { ChangePasswordRequest } from '../../models/Auth/ChangePasswordRequest';

const RecoveryPassword: React.FC = () => {
    const [transState, setTransState] = useState(false);
    const [passErrorMessage, setPassErrorMessage] = useState("");
    const [passOkMessage, setPassOkMessage] = useState("");
    const [password, setPassword] = useState("");
    const [passButtonEnabled, setPassButtonEnabled] = useState(false);
    const [showPass, setShowPass] = useState(false);
    const [isPasswordType, setIsPasswordType] = useState(true);
    const navigate = useNavigate();

    const [jwt, setJwt] = useState("");
    const [email, setEmail] = useState("");

    useEffect(() => {
        setTransState(true);
    }, []);

    useEffect(() => {
        try {
            const jwt = window.location.href.split(eps.fontend.passwordRecovery + '/')[1];
            console.info(`Parsing jwt from url: \'${jwt}\'.`);
            const claims = jwtDecode<IResetPasswordJwtInfo>(jwt);

            console.info(`Got email for password changing: ${claims.emailJwtClaim}.`);
            if (!claims.emailJwtClaim.includes("@")) {
                navigate("/404");
            }
            if (!JwtHelper.ValidateLifetime(claims)) {
                setPassErrorMessage("Your token has expired. Please request another to continue.");
                return;
            }

            setJwt(jwt);
            setEmail(claims.emailJwtClaim);
            setPassButtonEnabled(true);
        }
        catch {
            navigate("/404");
        }
    }, [])

    const transitionClasses = {
        enterActive: cla.welcomeTransitionEnter,
        enterDone: cla.welcomeTransitionEnterActive,
        exitActive: cla.welcomeTransitionExit,
        exitDone: cla.welcomeTransitionExitActive,
    }
    const commonTransProp = {
        timeout: 200,
        classNames: transitionClasses
    }

    const onSubmit = async () => {
        setPassButtonEnabled(false);
        setTimeout(() => setPassButtonEnabled(true), 5000);

        let passError = ValidationHelper.ValidatePasswordAndGetError(password);
        if (passError) {
            console.info("Password failed client-side validation.");
            setPassErrorMessage(passError);
            setPassButtonEnabled(true)
            return;
        }

        await ApiHelper.resetNewPassword(jwt, new ChangePasswordRequest(password));
        let success = ApiHelper.lastStatus &&
            (200 <= ApiHelper.lastStatus && ApiHelper.lastStatus <= 299);

        setPassErrorMessage("");
        if (success) {
            setPassOkMessage("Your password was changed. Now you can log in.");
            setPassButtonEnabled(false);
            setTimeout(() => setPassButtonEnabled(false), 5000);
        }
        else {
            setPassErrorMessage(`We was unable to change your password. Error code: HTTP_${ApiHelper.lastStatus}.`);
        }
    }


    return (
        <CSSTransition in={transState} {...commonTransProp}>
            <span className={cla.authWrapper}>
                <span className={cla.authHeader}>
                    <strong>Change password</strong>
                </span>
                <span className={[cla.authHeader,cla.credentialsLabel].join(' ')}>
                    You are about to change password for the account associated with email:
                </span>
                <input
                    onKeyUp={e => { if (e.key.toLowerCase() === "enter") onSubmit(); }}
                    onDragEnter={onSubmit}
                    type={"email"}
                    placeholder="Email"
                    value={email}
                    className={cla.credentialsInput} 
                    readOnly={true}/>
                <span className={cla.credentialsLabel}>Password</span>
                <input
                    onKeyUp={e => { if (e.key.toLowerCase() === "enter") onSubmit(); }}
                    type={"password"}
                    autoComplete="new-password"
                    placeholder="Password"
                    value={password} onChange={(e) => setPassword(e.target.value)}
                    className={cla.credentialsInput} />
                <button
                    type="submit"
                    onClick={onSubmit}
                    className={cla.authSubmit}
                    disabled={!passButtonEnabled}>SUBMIT</button>

                {passErrorMessage
                    ? <span className={cl.errorWrapper} style={{width:"100%"}}>
                        <span>{passErrorMessage}</span>
                    </span>
                    : <span />
                }
                {passOkMessage
                    ? <span className={cl.okWrapper} style={{width:"100%",cursor:"pointer"}} onClick={()=>{navigate("/auth")}}>
                        <span>{passOkMessage}</span>
                    </span>
                    : <span />
                }
            </span>
        </CSSTransition>);
}

export default RecoveryPassword;