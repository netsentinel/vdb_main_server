import { useState, useEffect, useMemo } from 'react';
import cl from "./VdbSecurity.module.css";
import clp from "../VdbPersonal/VdbPersonal.module.css";
import cla from "../AuthForm/AuthForm.module.css"
import EnvHelper from "src/helpers/EnvHelper";
import IUserDevice from '../../models/Device/IUserDevice';
import UserApiHelper from '../../helpers/UserApiHelper';
import DeviceCard from '../DeviceCard/DeviceCard';
import { CSSTransition } from 'react-transition-group';
import ISessionsResponse from '../../models/Auth/ISessionsResponse';
import { ChangePasswordRequest } from '../../models/Auth/ChangePasswordRequest';
import ValidationHelper from '../../helpers/ValidationHelper';
import { useNavigate } from 'react-router-dom';

const VdbSecurity: React.FC = () => {
    const [transState, setTransState] = useState(false);
    const [sessions, setSessions] = useState<ISessionsResponse>();
    const [terminationErrorMessage, setTerminationErrorMessage] = useState("");
    const [passErrorMessage, setPassErrorMessage] = useState("");
    const [passOkMessage, setPassOkMessage] = useState("");
    const [wasTerminated, setWasTerminated] = useState(false);
    const [passwordInput, setPasswordInput] = useState("");
    const [terminateButtonEnabled, setTerminateButtonEnabled] = useState(true);
    const [passButtonEnabled, setPassButtonEnabled] = useState(true);
    const [showPass, setShowPass] = useState(false);
    const [isPasswordType, setIsPasswordType] = useState(true);
    const navigate = useNavigate();

    useEffect(() => {
        setTransState(true);

        /* // ! Initially we let the browser there will be the password,
         * // ! after that, disable.
         * // ! https://stackoverflow.com/a/22457652/11325184
        */
        setIsPasswordType(false);
    }, []);

    useMemo(() => {
        new UserApiHelper()
            .getUserSessions().then(r => setSessions(r));
    }, []);

    const terminateOthers = async () => {
        setTerminateButtonEnabled(false);
        setTimeout(() => setTerminateButtonEnabled(true), 5000);

        let success = await new UserApiHelper()
            .terminateOtherSessions();

        setTerminationErrorMessage("");
        if (success) setWasTerminated(true);
        else setTerminationErrorMessage("We was unable to terminate your sessions.");
    }

    const setPassword = async () => {
        setPassButtonEnabled(false);
        setTimeout(() => setPassButtonEnabled(true), 5000);
        let passError = ValidationHelper.ValidatePasswordAndGetError(passwordInput);
        if (passError) {
            console.info("Password failed client-side validation.");
            setPassErrorMessage(passError);
            setPassButtonEnabled(true)
            return;
        }

        setIsPasswordType(true); // !
        let success = await new UserApiHelper()
            .changePassword(passwordInput);

        setPassErrorMessage("");
        if (success) {
            setPassOkMessage("Your password was changed.");
            //navigate("/auth");
            window.location.reload(); // needed for browser to offer password remember!
        }
        else {
            setPassErrorMessage("We was unable to change your password.");
            setIsPasswordType(false); // !
        }
    }

    const transitionClasses = {
        enterActive: cl.welcomeTransitionEnter,
        enterDone: cl.welcomeTransitionEnterActive,
        exitActive: cl.welcomeTransitionExit,
        exitDone: cl.welcomeTransitionExitActive,
    }
    const commonTransProp = {
        timeout: 200,
        classNames: transitionClasses
    }

    return (
        <CSSTransition in={transState} {...commonTransProp}>
            <span className={cl.wrapper}>
                {(!sessions || sessions.totalCount < 2 || wasTerminated)
                    ?
                    <span className={[clp.loggedAs, cl.menuWrapper].join(' ')}>
                        You have no other sessions.
                    </span>
                    :
                    <span className={cl.menuWrapper}>
                        <span className={clp.loggedAs}>
                            You have&nbsp;
                            <span className={clp.loggedAsEmail}>
                                {sessions.totalCount - 1} other&nbsp;
                                {sessions.totalCount > 2 ? "sessions" : "session"}
                            </span>
                        </span>
                        <button
                            className={clp.logoutBtn}
                            onClick={terminateOthers}
                            disabled={!terminateButtonEnabled}>
                            TERMINATE OTHERS
                        </button>
                    </span>
                }
                {terminationErrorMessage
                    ? <span className={cl.errorWrapper}>
                        <span>{terminationErrorMessage}</span>
                    </span>
                    : <span />
                }

                {
                    // ! Since non of the ways helped to disable unneeded 'save password' prompt here,
                    // ! this span is totaly disabled.
                }
                <span className={cl.menuWrapper} style={{ display: "none" }} hidden={true}>
                    <input
                        // https://stackoverflow.com/a/22457652/11325184
                        className={[clp.loggedAs, (showPass || !passwordInput) ? cl.newPasswordInput : cl.newPasswordInputHidden].join(" ")}
                        placeholder="new password"
                        value={passwordInput}
                        onChange={e => setPasswordInput(e.target.value)}
                        type={isPasswordType ? "password" : "text"}
                        autoComplete="new-password"
                    />
                    <button
                        className={[clp.logoutBtn, cl.hideShowBtn].join(' ')}
                        onClick={() => setShowPass(!showPass)}
                        disabled={!passButtonEnabled}
                        type="button">
                        {showPass ? "HIDE" : "SHOW"}
                    </button>
                    <button
                        className={clp.logoutBtn}
                        onClick={setPassword}
                        disabled={!passButtonEnabled}
                        type="button">
                        SET PASSWORD
                    </button>
                </span>
                {passErrorMessage
                    ? <span className={cl.errorWrapper}>
                        <span>{passErrorMessage}</span>
                    </span>
                    : <span />
                }
                {passOkMessage
                    ? <span className={cl.okWrapper}>
                        <span>{passOkMessage}</span>
                    </span>
                    : <span />
                }

            </span>
        </CSSTransition >);
}

export default VdbSecurity;