import cl from "../AuthForm/AuthForm.module.css";
import { NavLink, useNavigate } from 'react-router-dom';
import GlobalContext from '../../helpers/GlobalContext';
import { CSSTransition } from 'react-transition-group';
import { Route, Routes } from "react-router-dom";
import VdbWelcome from '../VdbWelcome/VdbWelcome';
import { useState, useEffect, useMemo } from 'react';
import EnvHelper from '../../helpers/EnvHelper';
import AuthHelper from '../../helpers/AuthHelper';
import authSettings from "../../config/authSettings.json"
import RegistrationRequest from '../../models/Auth/RegistrationRequest';
import ValidationHelper from '../../helpers/ValidationHelper';
import eps from '../../config/endpoints.json';
import ApiHelper from '../../helpers/ApiHelper';
import { STATUS_CODES } from "http";

const ForgotPassword: React.FC = () => {
    const navigate = useNavigate();
    const [transState, setTransState] = useState(false);
    const [email, setEmail] = useState("");
    const [submitEnabled, setSubmitEnabled] = useState(true);
    const [errorMessage, setErrorMessage] = useState("");
    const [okMessage, setOkMessage] = useState("");

    useEffect(() => {
        setTransState(true);
    }, []);

    useMemo(() => {
        AuthHelper.EnsureUserInContext().then(r => {
            if (r && GlobalContext.currentUser !== undefined) {
                navigate("/personal");
            }
        });
    }, []);

    const onSubmit = async () => {
        if (EnvHelper.isDebugMode())
            setErrorMessage("Test erorr message");

        let emailError = ValidationHelper.ValidateEmailAndGetError(email);
        if (emailError) {
            console.info("Email failed client-side validation.");
            setErrorMessage(emailError);
            setSubmitEnabled(true);
            return;
        }


        if (!EnvHelper.isDebugMode()) {
            setSubmitEnabled(false);
            if (!EnvHelper.isDebugMode()) setTimeout(() => setSubmitEnabled(true), 5000);
        }
        setErrorMessage("");
        console.info(`Submitting: ${email}`);


        try {
            await ApiHelper.resetPassword(email);
        } catch { }

        if (ApiHelper.lastStatus) {
            let status = ApiHelper.lastStatus ?? -1;
           // console.info(`Last status is ${ApiHelper.GetLastStatus()}.`);
            let selectedError: string;

            if (200 <= status && status <= 299) {
                setErrorMessage("");
                setOkMessage("Ok. We send link to your inbox. Please follow it to change your password.");
                console.info("Ok. We send link to your inbox. Please follow it to change your password.");
                setSubmitEnabled(false);
                if (!EnvHelper.isDebugMode()) setTimeout(() => setSubmitEnabled(false), 5000);
                return;
            }
            else if (status === 400)
                selectedError = "Problem on client side. Please reload window.";
            else if (status === 401)
                selectedError = "Sorry, server refused to service your request.";
            else if (status === 404)
                selectedError = "Server was unable to find existing account.";
            else if (status === 409)
                selectedError = "Server has found existing account but refused to authorize it.";
            else if (status == 503)
                selectedError = "We are currently unable to service your request. Please try later.";
            else if (status >= 500)
                selectedError = "Problem on server side. Please try later.";
            else {
                selectedError = "Unable to send data to server.";
            }

            setErrorMessage(selectedError + ` Error code: HTTP_${status}.`);
        }
        else {
            setErrorMessage("Unable to send data to server.")
        }

        setSubmitEnabled(true);
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
            <span className={cl.authWrapper}>
                <span className={cl.authHeader}>
                    <strong>Reset password</strong>
                </span>
                <span className={cl.credentialsLabel}>Email</span>
                <input
                    onKeyUp={e => { if (e.key.toLowerCase() === "enter") onSubmit(); }}
                    onDragEnter={onSubmit}
                    type={"email"}
                    placeholder="Email"
                    value={email} onChange={(e) => setEmail(e.target.value)}
                    className={cl.credentialsInput} />
                <button
                    type="submit"
                    onClick={onSubmit}
                    className={cl.authSubmit}
                    disabled={!submitEnabled}>SUBMIT</button>
                {errorMessage
                    ? <span className={cl.errorWrapper}>
                        <span>{errorMessage}</span>
                    </span>
                    : <span />
                }
                {okMessage
                    ? <span className={cl.errorWrapper} style={{backgroundColor:"rgb(240, 255, 240)", border:"1px solid green"}}>
                        <span>{okMessage}</span>
                    </span>
                    : <span />
                }
            </span>
        </CSSTransition>
    );
}

export default ForgotPassword;